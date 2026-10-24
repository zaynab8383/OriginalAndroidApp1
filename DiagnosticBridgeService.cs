using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Usb;
using Android.OS;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace AndroidApp1;

[Service(
    Name = "com.companyname.AndroidApp1.DiagnosticBridgeService",
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeConnectedDevice)]
public sealed class DiagnosticBridgeService : Service
{
    public const string LogAction = "com.companyname.AndroidApp1.BRIDGE_LOG";
    public const string LogExtra = "message";

    private const string PreferencesName = "bridge_preferences";
    private const string UserStoppedKey = "bridge_user_stopped";
    private const string ChannelId = "diagnostic_bridge_channel";
    private const int NotificationId = 9000;
    private const string ServerIp = "192.168.70.177";
    private const int ServerPort = 9000;
    private const int MaxReceiveBufferBytes = 128 * 1024;

    private readonly ConcurrentQueue<byte[]> _usbTransmitQueue = new();
    //private readonly Channel<byte[]> _tcpTransmitChannel =
    //    Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    //    {
    //        SingleReader = true,
    //        SingleWriter = false
    //    });
    private readonly TcpFrameQueue _tcpQueue = new();
    private volatile bool _tcpConnected;

    [System.Diagnostics.Conditional("VERBOSE_LOG")]
    private void LogVerbose(string message) => Log(message);

    private static string? GetCoalesceKey(byte[] frame) => null;
    private CancellationTokenSource? _lifetime;
    private Task? _usbTask;
    private Task? _tcpTask;
    private UsbCdcService? _usb;
    private UsbDevice? _usbDevice;
    //private bool _usbOpened;
    private volatile bool _usbOpened;
    private PowerManager.WakeLock? _wakeLock;
    private bool _running;
    private string _usbState = "USB: searching";
    private string _tcpState = "TCP: connecting";

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var preferences = GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        if (preferences?.GetBoolean(UserStoppedKey, false) == true)
        {
            StopSelf(startId);
            return StartCommandResult.NotSticky;
        }

        StartForegroundCompat(BuildNotification("Starting USB/TCP bridge"));

        if (!_running)
        {
            _running = true;
            AcquireCpuWakeLock();
            _lifetime = new CancellationTokenSource();
            var token = _lifetime.Token;
            _usbTask = Task.Run(() => UsbLoopAsync(token));
            _tcpTask = Task.Run(() => TcpLoopAsync(token));
            Log("Bridge service started.");
        }

        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    private void StartForegroundCompat(Notification notification)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(NotificationId, notification, ForegroundService.TypeConnectedDevice);
        else
            StartForeground(NotificationId, notification);
    }

    // Preserves the original app's USB -> frame -> TCP behavior and visible data log.
    private async Task UsbLoopAsync(CancellationToken token)
    {
        var receiveBuffer = new List<byte>(4096);
        byte[]? pendingUsbCommand = null;
        bool usbFailureLogged = false;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_usb == null || _usbDevice == null || !_usbOpened)
                {
                    _usb ??= new UsbCdcService();
                    var found = _usb.FindCdcDevice();

                    if (found == null)
                    {
                        SetUsbState("USB: no CDC port found");
                        await Task.Delay(1000, token);
                        continue;
                    }

                    Log("Port name: " + found.DeviceName);

                    if (!_usb.HasPermission(found))
                    {
                        _usbDevice = null;
                        _usb.RequestPermission(found);
                        SetUsbState("USB: waiting for permission");
                        Log("USB permission requested.");
                        await Task.Delay(1000, token);
                        continue;
                    }

                    _usbDevice = found;
                    if (!_usb.OpenDevice(found))
                    {
                        Log("USB port could not be opened; retrying.");
                        CloseUsb();
                        await Task.Delay(1000, token);
                        continue;
                    }

                    _usbOpened = true;
                    SetUsbState("USB: connected - " + found.DeviceName);
                    Log("USB PORT Opened OK");
                    while (_usbTransmitQueue.TryDequeue(out _)) { }
                    receiveBuffer.Clear();
                }

                // Detect detach/replacement without depending on Activity lifetime.
                var current = _usb.FindCdcDevice();
                if (current == null || current.DeviceName != _usbDevice.DeviceName)
                {
                    Log("USB device disconnected.");
                    CloseUsb();
                    receiveBuffer.Clear();
                    SetUsbState("USB: searching");
                    await Task.Delay(500, token);
                    continue;
                }

                var bytes = _usb.Read_Byte(10);
                if (bytes is { Length: > 0 })
                {
                    LogVerbose($"USB RX ({bytes.Length} bytes): {BitConverter.ToString(bytes)}");
                    receiveBuffer.AddRange(bytes);
                    ExtractAndForwardCompleteFrames(receiveBuffer);

                    if (receiveBuffer.Count > MaxReceiveBufferBytes)
                    {
                        Log("USB receive buffer exceeded 128 KB; clearing incomplete frame.");
                        receiveBuffer.Clear();
                    }
                }

                //while (_usbTransmitQueue.TryDequeue(out var toUsb))
                //{
                //    if (toUsb.Length == 0)
                //        continue;

                //    //_usb.Write_Byte(toUsb, 300);
                //    if (!_usb.Write_Byte(toUsb, 300))
                //        Log("USB write failed (" + toUsb.Length + " bytes) - command lost");
                //    LogVerbose("TCP Client -> USB SND LEN: " + toUsb.Length);
                // }
                if (pendingUsbCommand == null)
                {
                    if (_usbTransmitQueue.TryDequeue(out var nextCommand))
                        pendingUsbCommand = nextCommand;
                }

                if (pendingUsbCommand != null)
                {
                    if (pendingUsbCommand.Length == 0)
                    {
                        pendingUsbCommand = null;
                        usbFailureLogged = false;
                    }
                    else if (_usb.Write_Byte(pendingUsbCommand, 300))
                    {
                        Log("USB write OK (" + pendingUsbCommand.Length + " bytes)");
                        LogVerbose(
                            "TCP Client -> USB SND LEN: " +
                            pendingUsbCommand.Length);

                        pendingUsbCommand = null;
                        usbFailureLogged = false;
                    }
                    else
                    {
                        if (!usbFailureLogged)
                        {
                            Log(
                                "USB write failed (" +
                                pendingUsbCommand.Length +
                                " bytes) - retrying command");
                            usbFailureLogged = true;
                        }

                        // فرمان حذف نمی‌شود؛ در دور بعد دوباره امتحان می‌شود.
                        await Task.Delay(100, token);
                    }
                }
            }
            catch (System.OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log("USB error: " + ex.Message);
                CloseUsb();
                receiveBuffer.Clear();
                SetUsbState("USB: reconnecting");
                try { await Task.Delay(1000, token); }
                catch (System.OperationCanceledException) { break; }
            }
        }

        CloseUsb();
    }

    private void ExtractAndForwardCompleteFrames(List<byte> buffer)
    {
        // Original protocol frame terminator: E2 97 89. Keep all bytes through each terminator.
        while (buffer.Count >= 3)
        {
            var endExclusive = -1;
            for (var i = 0; i <= buffer.Count - 3; i++)
            {
                if (buffer[i] == 0xE2 && buffer[i + 1] == 0x97 && buffer[i + 2] == 0x89)
                {
                    endExclusive = i + 3;
                    break;
                }
            }

            if (endExclusive < 0)
                return;

            var frame = buffer.GetRange(0, endExclusive).ToArray();
            buffer.RemoveRange(0, endExclusive);
            if (IsIdentityFrame(frame))
                _lastIdentityFrame = frame;

            //_tcpTransmitChannel.Writer.TryWrite(frame);
            //var text = Encoding.UTF8.GetString(frame);
            //Log($"Count: {frame.Length}\n{text}");
            if (_tcpConnected)
                _tcpQueue.Enqueue(frame, GetCoalesceKey(frame));

            LogVerbose($"Count: {frame.Length}\n{Encoding.UTF8.GetString(frame)}");
        }
    }

    private async Task TcpLoopAsync(CancellationToken token)
    {
        var retryDelayMs = 1000;

        while (!token.IsCancellationRequested)
        {
            TcpClient? client = null;
            CancellationTokenSource? connectionLifetime = null;

            try
            {
                // Connect only while the USB link to the diag is open, so the server's
                // identity probe ("-11") always reaches a ready diag.
                if (!_usbOpened)
                {
                    SetTcpState("TCP: waiting for USB");
                    await Task.Delay(500, token);
                    continue;
                }

                SetTcpState($"TCP: connecting to {ServerIp}:{ServerPort}");
                Log($"Connecting to {ServerIp}:{ServerPort}");

                client = new TcpClient { NoDelay = true };
                ConfigureKeepAlive(client.Client);
                await client.ConnectAsync(ServerIp, ServerPort, token);
                retryDelayMs = 1000;

                if (!_usbOpened)
                    continue; // USB vanished while connecting; finally{} closes the socket.

                SetTcpState("TCP: connected");
                Log("TCP Client Connected...");
                _tcpQueue.Clear();
                while (_usbTransmitQueue.TryDequeue(out _)) { } // drop stale server commands
                _tcpConnected = true;
                var identity = _lastIdentityFrame;
                if (identity != null)
                    _tcpQueue.Enqueue(identity); // announce the diag immediately after (re)connect

                connectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
                var ct = connectionLifetime.Token;
                var stream = client.GetStream();
                var sendTask = TcpSendLoopAsync(stream, ct);
                var receiveTask = TcpReceiveLoopAsync(stream, ct);
                var usbWatchTask = WatchUsbAsync(ct);

                await Task.WhenAny(sendTask, receiveTask, usbWatchTask);
                connectionLifetime.Cancel();
                client.Close();
                if (!_usbOpened)
                    _lastIdentityFrame = null;


                try { await Task.WhenAll(sendTask, receiveTask, usbWatchTask); }
                catch (System.OperationCanceledException) { }
                catch (IOException) { }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }

                if (!token.IsCancellationRequested)
                    Log(_usbOpened ? "TCP Client Disconnected; reconnecting." : "USB lost; TCP closed.");
            }
            catch (System.OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    Log("TCP connection error: " + ex.Message);
            }
            finally
            {
                _tcpConnected = false;
                connectionLifetime?.Cancel();
                connectionLifetime?.Dispose();
                client?.Close();
            }

            if (token.IsCancellationRequested) break;

            SetTcpState("TCP: disconnected; retrying");
            try { await Task.Delay(retryDelayMs, token); }
            catch (System.OperationCanceledException) { break; }
            retryDelayMs = Math.Min(retryDelayMs * 2, 15000);
        }
    }

    //private async Task WatchUsbAsync(CancellationToken token)
    //{
    //    while (!token.IsCancellationRequested && _usbOpened)
    //        await Task.Delay(300, token);
    //}
    private static readonly TimeSpan UsbGracePeriod = TimeSpan.FromSeconds(10);
    private volatile byte[]? _lastIdentityFrame;

    private static bool IsIdentityFrame(byte[] frame)
    {
        if (frame.Length > 512) return false;
        var text = Encoding.UTF8.GetString(frame);
        return text.Contains("Serial Number") && text.Contains("◆-11◆");
    }
    private async Task WatchUsbAsync(CancellationToken token)
    {
        var lostSince = DateTime.MinValue;

        while (!token.IsCancellationRequested)
        {
            if (_usbOpened)
            {
                lostSince = DateTime.MinValue;
            }
            else if (lostSince == DateTime.MinValue)
            {
                lostSince = DateTime.UtcNow;
            }
            else if (DateTime.UtcNow - lostSince > UsbGracePeriod)
            {
                return; // USB really gone: close TCP so the server marks the diag offline.
            }

            await Task.Delay(300, token);
        }
    }

    private static void ConfigureKeepAlive(Socket socket)
    {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        try
        {
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        }
        catch { /* not supported on some devices */ }
    }

    //private async Task TcpLoopAsync(CancellationToken token)
    //{
    //    var retryDelayMs = 1000;

    //    while (!token.IsCancellationRequested)
    //    {
    //        TcpClient? client = null;
    //        CancellationTokenSource? connectionLifetime = null;

    //        try
    //        {
    //            SetTcpState($"TCP: connecting to {ServerIp}:{ServerPort}");
    //            Log($"Connecting to {ServerIp}:{ServerPort}");

    //            client = new TcpClient { NoDelay = true };
    //            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    //            await client.ConnectAsync(ServerIp, ServerPort, token);
    //            retryDelayMs = 1000;
    //            SetTcpState("TCP: connected");
    //            Log("TCP Client Connected...");
    //            _tcpQueue.Clear();
    //            _tcpConnected = true;
    //            connectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
    //            var stream = client.GetStream();
    //            var sendTask = TcpSendLoopAsync(stream, connectionLifetime.Token);
    //            var receiveTask = TcpReceiveLoopAsync(stream, connectionLifetime.Token);

    //            await Task.WhenAny(sendTask, receiveTask);
    //            connectionLifetime.Cancel();
    //            client.Close();

    //            try { await Task.WhenAll(sendTask, receiveTask); }
    //            catch (System.OperationCanceledException) { }
    //            catch (IOException) { }
    //            catch (SocketException) { }

    //            if (!token.IsCancellationRequested)
    //                Log("TCP Client Disconnected; reconnecting.");
    //        }
    //        catch (System.OperationCanceledException) when (token.IsCancellationRequested)
    //        {
    //            break;
    //        }
    //        catch (Exception ex)
    //        {
    //            if (!token.IsCancellationRequested)
    //                Log("TCP connection error: " + ex.Message);
    //        }
    //        finally
    //        {
    //            _tcpConnected = false;
    //            connectionLifetime?.Cancel();
    //            connectionLifetime?.Dispose();
    //            client?.Close();
    //        }

    //        SetTcpState("TCP: disconnected; retrying");
    //        try { await Task.Delay(retryDelayMs, token); }
    //        catch (System.OperationCanceledException) { break; }
    //        retryDelayMs = Math.Min(retryDelayMs * 2, 15000);
    //    }
    //}

    //private async Task TcpSendLoopAsync(NetworkStream stream, CancellationToken token)
    //{
    //    await foreach (var data in _tcpTransmitChannel.Reader.ReadAllAsync(token))
    //    {
    //        await stream.WriteAsync(data, token);
    //        await stream.FlushAsync(token);
    //        Log("TCP Client SND LEN: " + data.Length);
    //    }
    //}
    private async Task TcpSendLoopAsync(NetworkStream stream, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = await _tcpQueue.DequeueAsync(token);
            await stream.WriteAsync(data, token);
            LogVerbose("TCP Client SND LEN: " + data.Length);
        }
    }
    private async Task TcpReceiveLoopAsync(NetworkStream stream, CancellationToken token)
    {
        var buffer = new byte[10000];
        while (!token.IsCancellationRequested)
        {
            var count = await stream.ReadAsync(buffer, token);
            if (count == 0)
                throw new IOException("Server closed the TCP connection.");

            var received = new byte[count];
            Array.Copy(buffer, received, count);
            _usbTransmitQueue.Enqueue(received);
            LogVerbose("TCP Client RCV LEN: " + count);
        }
    }

    private void CloseUsb()
    {
        try { _usb?.Close(); } catch { }
        _usb = null;
        _usbDevice = null;
        _usbOpened = false;
    }

    private void SetUsbState(string value)
    {
        _usbState = value;
        UpdateNotification();
    }

    private void SetTcpState(string value)
    {
        _tcpState = value;
        UpdateNotification();
    }

    private void Log(string message)
    {
        var intent = new Intent(LogAction);
        intent.SetPackage(PackageName);
        intent.PutExtra(LogExtra, message);
        SendBroadcast(intent);
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var channel = new NotificationChannel(ChannelId, "Diagnostic bridge", NotificationImportance.Low)
        {
            Description = "Keeps USB diagnostic and TCP server communication active."
        };
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification(string content)
    {
        var openIntent = new Intent(this, typeof(MainActivity));
        openIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pendingIntent = PendingIntent.GetActivity(
            this, 0, openIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
            ? new Notification.Builder(this, ChannelId)
            : new Notification.Builder(this);

        builder.SetContentTitle("AndroidApp1 Diagnostic Bridge")
            .SetContentText(content)
            .SetStyle(new Notification.BigTextStyle().BigText(_usbState + "\n" + _tcpState))
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetContentIntent(pendingIntent);

        return builder.Build();
    }

    private void UpdateNotification()
    {
        try
        {
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.Notify(NotificationId, BuildNotification(_usbState + "; " + _tcpState));
        }
        catch { }
    }

    private void AcquireCpuWakeLock()
    {
        try
        {
            var powerManager = (PowerManager?)GetSystemService(PowerService);
            if (powerManager == null) return;
            _wakeLock ??= powerManager.NewWakeLock(WakeLockFlags.Partial, "AndroidApp1:DiagnosticBridge");
            if (!_wakeLock.IsHeld) _wakeLock.Acquire();
        }
        catch (Exception ex) { Log("Wake lock error: " + ex.Message); }
    }

    public override void OnDestroy()
    {
        _running = false;
        try { _lifetime?.Cancel(); } catch { }
        CloseUsb();
        try { if (_wakeLock?.IsHeld == true) _wakeLock.Release(); } catch { }
        _wakeLock = null;
        _lifetime?.Dispose();
        _lifetime = null;
        Log("Bridge service stopped.");
        base.OnDestroy();
    }
}
