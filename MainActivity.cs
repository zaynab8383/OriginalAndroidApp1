//using Android.App;
//using Android.Content;
//using Android.Hardware.Usb;
//using Android.Health.Connect.DataTypes.Units;
//using Android.OS;
//using Android.Widget;
//using Java.IO;
//using System;
////using Hoho.Android.UsbSerial.Driver;
//using System.Collections.Generic;
//using System.Net.Sockets;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using static Android.Graphics.ColorSpace;

//namespace AndroidApp1
//{

//    //==============================================================================

//    [Activity(Label = "@string/app_name", MainLauncher = true)]
//    public class MainActivity : Activity
//    {
//        UsbCdcService usb;
//        Android.Hardware.Usb.UsbDevice device;

//        //private TcpClient _client;
//        //private NetworkStream _stream;

//        Thread USBThread;
//        Thread TCPThread;

//        TextView myTextView;

//        //=============================================================================================

//        UsbBroadcastReceiver receiver;

//        protected override void OnCreate(Bundle? savedInstanceState)
//        {
//            base.OnCreate(savedInstanceState);
//            Window?.AddFlags(
//                Android.Views.WindowManagerFlags.KeepScreenOn);

//            // Set our view from the "main" layout resource
//            SetContentView(Resource.Layout.activity_main);


//            //-------------------------------------------------------------------

//            receiver = new UsbBroadcastReceiver();

//            IntentFilter filter = new IntentFilter();
//            filter.AddAction(UsbManager.ActionUsbDeviceAttached);
//            filter.AddAction(UsbManager.ActionUsbDeviceDetached);

//            RegisterReceiver(receiver, filter);

//            //-------------------------------------------------------------------


//            Button myButton = FindViewById<Button>(Resource.Id.myButton);
//            myButton.Click += OnMyButtonClick;

//            Button Open_BTN = FindViewById<Button>(Resource.Id.Open_BTN);
//            Open_BTN.Click += On_Open_BTN_Click;

//            Button Clear_BTN = FindViewById<Button>(Resource.Id.Clear_BTN);
//            Clear_BTN.Click += On_Clear_BTN_Click;




//            myTextView = FindViewById<TextView>(Resource.Id.myTextView);
//            myTextView.MovementMethod = new Android.Text.Method.ScrollingMovementMethod();
//            myTextView.Text = "Text changed from C#";


//            // اول تمام منابع مشترک را مقداردهی اولیه کن
//            TCP_SND_BYTE_List = new List<byte>();
//            USB_SND_BYTE_List = new List<byte>();

//            Thread_USB_Step = 0;
//            Thread_USB_Terminate = false;

//            Thread_TCP_Step = 3;
//            Thread_TCP_Terminate = false;

//            // تردها فقط بعد از مقداردهی اولیه شروع شوند
//            USBThread = new Thread(USB_Thread);
//            TCPThread = new Thread(TCP_Thread);

//            USBThread.Start();
//            TCPThread.Start();


//            //Thread_USB_Step = 0;
//            //Thread_USB_Terminate = false;
//            //USBThread = new Thread(USB_Thread);
//            //USBThread.Start();

//            //// USBThread.Abort();


//            //Thread_TCP_Step = 0;
//            //Thread_TCP_Terminate = false;
//            //TCPThread = new Thread(TCP_Thread);
//            //TCPThread.Start();


//            //TCP_SND_BYTE_List = new List<byte>();
//            //TCP_SND_BYTE_List.Clear();


//            //USB_SND_BYTE_List = new List<byte>();
//            //USB_SND_BYTE_List.Clear();

//            //TCP_SND_BYTE_List = new List<byte>();
//            //USB_SND_BYTE_List = new List<byte>();

//            //Thread_USB_Step = 0;
//            //Thread_USB_Terminate = false;
//            //USBThread = new Thread(USB_Thread);
//            //USBThread.Start();

//            //Thread_TCP_Step = 3;
//            //Thread_TCP_Terminate = false;
//            //TCPThread = new Thread(TCP_Thread);
//            //TCPThread.Start();

//            //_stream = null;


//        }
//        protected override void OnDestroy()
//        {
//            Thread_TCP_Terminate = true;
//            Thread_USB_Terminate = true;



//            base.OnDestroy();


//            UnregisterReceiver(receiver);

//            //USBThread.Abort();
//            //TCPThread.Abort();

//            // Your cleanup code here
//        }

//        //=============================================================================================
//        private void On_Clear_BTN_Click(object sender, System.EventArgs e)
//        {
//            myTextView.Text = "";
//        }

//        private void On_Open_BTN_Click(object sender, System.EventArgs e)
//        {
//            /*
//            if (device == null)
//            {
//                myTextView.Append("\nNO PORT Exist...!");

//                //Toast.MakeText(this, "NO PORT Exist...!", ToastLength.Long).Show();
//                return;
//            }

//            usb.OpenDevice(device);
//            myTextView.Append("\nPORT Opened");
//            //Toast.MakeText(this, "PORT Opened", ToastLength.Long).Show();
//            Thread_USB_Step = 1;
//            */
//        }

//        private void OnMyButtonClick(object sender, System.EventArgs e)
//        {
//            usb = new UsbCdcService();

//            device = usb.FindCdcDevice();

//            if (device == null)
//            {
//                //Toast.MakeText(this, "NO PORT Exist...!", ToastLength.Long).Show();
//                myTextView.Append("\nNO PORT Exist...!");
//            }
//            else
//            {
//                myTextView.Append("\nPort name: " + device.DeviceName);
//                //Toast.MakeText(this, device.DeviceName, ToastLength.Long).Show();
//                //Port_Name = device.DeviceName;
//                if (usb.OpenDevice(device) == true)
//                {
//                    myTextView.Append("\nPORT Opened");
//                    //Toast.MakeText(this, "PORT Opened", ToastLength.Long).Show();
//                    Thread_USB_Step = 1;
//                }
//            }
//        }

//        //=============================================================================================
//        bool Thread_USB_Terminate;
//        int Thread_USB_Step = 0;
//        byte[] SND_Data;


//        private bool USB_Connect()
//        {
//            usb = new UsbCdcService();

//            device = usb.FindCdcDevice();

//            if (device == null)
//            {
//                Thread_TEXT_LIST_Append("NO PORT Exist...!");
//            }
//            else
//            {
//                Thread_TEXT_LIST_Append("Port name: " + device.DeviceName);
//                if (usb.OpenDevice(device) == true)
//                {
//                    Thread_TEXT_LIST_Append("USB PORT Opened OK");
//                    return true;
//                }
//            }

//            return false;
//        }
//        private void USB_SND(byte[] data)
//        {
//            USB_SND_BYTE_List.AddRange(data);
//        }

//        private void Thread_TEXT_LIST_Append(string Text)
//        {
//            RunOnUiThread(() =>
//            {
//                myTextView.Append("\n" + Text);
//            });
//        }



//        private void USB_Thread()
//        {
//            string TempStr = "";
//            byte[] RCV_Bytes = null;
//            Thread_USB_Step = 0;
//            Thread_USB_Terminate = false;
//            int Count;


//            List<byte> My_BYTE_List = new List<byte>();
//            My_BYTE_List.Clear();


//            usb = new UsbCdcService();


//            while (!Thread_USB_Terminate)
//            {

//                switch (Thread_USB_Step)
//                {
//                    case 0:

//                        device = usb.FindCdcDevice();

//                        if (device == null)
//                        {
//                            Thread_TEXT_LIST_Append("NO PORT Exist...!");
//                        }
//                        else
//                        {
//                            Thread_TEXT_LIST_Append("Port name: " + device.DeviceName);
//                            if (usb.OpenDevice(device) == true)
//                            {
//                                Thread_TEXT_LIST_Append("USB PORT Opened OK");
//                                receiver.USB_Connected = true;
//                                Thread_USB_Step = 1;
//                            }
//                        }

//                        Thread.Sleep(1000);




//                        /*
//                        if (receiver.USB_Connected == true)
//                        {
//                            if (USB_Connect() == true)
//                            {
//                                Thread_USB_Step = 1;
//                            }
//                        }
//                        */
//                        break;


//                    case 1:

//                        if (receiver.USB_Connected == true)
//                        {
//                            try
//                            {
//                                RCV_Bytes = usb.Read_Byte(10);

//                                if (RCV_Bytes != null && RCV_Bytes.Length > 0)
//                                {
//                                    Thread_TEXT_LIST_Append(
//                                        $"USB RX ({RCV_Bytes.Length} bytes): " +
//                                        BitConverter.ToString(RCV_Bytes));
//                                }

//                                if (RCV_Bytes != null)
//                                {
//                                    My_BYTE_List.AddRange(RCV_Bytes.ToArray());
//                                    for (int i = 0; i < My_BYTE_List.Count - 2; i++)
//                                    {
//                                        //E2 97 89
//                                        if (My_BYTE_List[i] == 0xE2)
//                                        {
//                                            if (My_BYTE_List[i + 1] == 0x97)
//                                            {
//                                                if (My_BYTE_List[i + 2] == 0x89)
//                                                {
//                                                    TempStr = Encoding.UTF8.GetString(My_BYTE_List.ToArray());
//                                                    Count = My_BYTE_List.ToArray().Length;

//                                                    TCP_Client_SND(My_BYTE_List.ToArray());

//                                                    RunOnUiThread(() =>
//                                                    {
//                                                        myTextView.Text = "Count: " + Count.ToString();

//                                                        myTextView.Append("\n" + TempStr);

//                                                        //myTextView.Text = TempStr;


//                                                        //Toast.MakeText(this, received, ToastLength.Long).Show();
//                                                    });

//                                                    My_BYTE_List.Clear();
//                                                }
//                                            }
//                                        }

//                                        ////My_BYTE_List.CopyTo();

//                                        //My_BYTE_List.RemoveRange();
//                                    }


//                                }

//                                //=============================================
//                                if (USB_SND_BYTE_List.Count > 0)
//                                {
//                                    RCV_Bytes = new byte[USB_SND_BYTE_List.Count];
//                                    USB_SND_BYTE_List.CopyTo(RCV_Bytes);
//                                    USB_SND_BYTE_List.Clear();

//                                    Thread_TEXT_LIST_Append("USB SND LEN: " + RCV_Bytes.Length);
//                                    usb.Write_Byte(RCV_Bytes, 300);
//                                }

//                                //=============================================

//                            }
//                            catch
//                            {
//                            }

//                        }
//                        else
//                        {
//                            Thread_TEXT_LIST_Append("USB device disconnected");
//                            Thread_USB_Step = 0;
//                        }

//                        break;

//                    case 2:
//                        if (SND_Data != null)
//                        {
//                            //Port_SND_Byte(SND_Data, 100);
//                        }

//                        Thread_USB_Step = 1;
//                        break;
//                }
//            }
//        }

//        //=============================================================================================
//        bool Thread_TCP_Terminate;
//        int Thread_TCP_Step = 0;

//        //string host = "192.168.100.11";
//        //int port = 50000;

//        private const string ServerIp = "192.168.70.177";
//        private const int ServerPort = 9000;

//        List<byte> TCP_SND_BYTE_List;
//        List<byte> USB_SND_BYTE_List;

//        bool TCP_CLIENT_CONNECTED = false;

//        private bool TCP_Client_Connect()
//        {
//            byte[] RCV_Bytes = null;
//            string TempStr = "";


//            TcpClientService ll = new TcpClientService();

//            ll.ConnectAsync(ServerIp, ServerPort);

//            while ((!Thread_TCP_Terminate) && (Thread_TCP_Step == 3))
//            {
//                if (ll.Client_Connected == true)
//                {

//                    Thread_TEXT_LIST_Append("TCP Client Connected...");
//                    break;
//                }
//                else
//                {
//                    Thread_TEXT_LIST_Append("TCP Client NOT Connected");
//                }

//                Thread.Sleep(1000);
//            }

//            while ((!Thread_TCP_Terminate) && (Thread_TCP_Step == 3))
//            {
//                ll.ReceiveAsync();

//                while ((!Thread_TCP_Terminate) && (Thread_TCP_Step == 3) && (ll.RCV_LEN == -2))
//                {
//                    if (TCP_SND_BYTE_List.Count > 0)
//                    {
//                        RCV_Bytes = new byte[TCP_SND_BYTE_List.Count];
//                        TCP_SND_BYTE_List.CopyTo(RCV_Bytes);
//                        TCP_SND_BYTE_List.Clear();

//                        Thread_TEXT_LIST_Append("TCP Client SND LEN: " + RCV_Bytes.Length);
//                        ll.SendAsync(RCV_Bytes);
//                    }

//                    Thread.Sleep(10);
//                }



//                Thread_TEXT_LIST_Append("TCP Client RCV LEN: " + ll.RCV_LEN.ToString());
//                if (ll.RCV_LEN > 0)
//                {
//                    if (ll.RCV_LEN != null)
//                    {
//                        USB_SND(ll.RCV_Bytes);
//                    }
//                }
//                else if (ll.RCV_LEN == -3)
//                {
//                    break;
//                }
//            }


//            Thread.Sleep(500);
//            //ll.Disconnect();
//            Thread_TEXT_LIST_Append("TCP Client Disconected");









//            return true;















//            /*
//            byte[] RCV_Bytes = null;
//            string TempStr = "";


//            myTextView.Append("\nTCP Client started. Connecting to server...");
//            try
//            {
//                using (var client = new TcpClient())
//                {
//                    client.Connect(ServerIp, ServerPort);

//                    client.Connect(ServerIp, ServerPort);
//                    myTextView.Append($"\nConnected to server at {ServerIp}:{ServerPort}");


//                    var stream = client.GetStream();

//                    client.ReceiveTimeout = 100;
//                    byte[] buffer = new byte[20000];
//                    int bytesRead = 0;


//                    TCP_CLIENT_CONNECTED = true;

//                    while ((client.Connected) && (!Thread_TCP_Terminate) && (Thread_TCP_Step == 3))
//                    {
//                        try
//                        {


//                            if (TCP_SND_BYTE_List.Count > 0)
//                            {
//                                RCV_Bytes = new byte[TCP_SND_BYTE_List.Count];
//                                TCP_SND_BYTE_List.CopyTo(RCV_Bytes);
//                                TCP_SND_BYTE_List.Clear();

//                                TempStr = Encoding.UTF8.GetString(RCV_Bytes);
//                                myTextView.Append("\nSND: " + TempStr);

//                                stream.Write(RCV_Bytes);
//                            }



//                            bytesRead = stream.ReadAsync(buffer, 0, buffer.Length);
//                        }
//                        catch (Exception)
//                        {
//                            bytesRead = 0;
//                        }

//                        if (bytesRead > 0)
//                        {
//                            RCV_Bytes = new byte[bytesRead];
//                            Array.Copy(buffer, RCV_Bytes, bytesRead);

//                            myTextView.Append("\nTCP bytesRead: " + bytesRead.ToString());
//                        }
//                        else
//                        {

//                            //Counter += client.ReceiveTimeout;
//                            //if (Counter > 2000)
//                            //{
//                            //    Counter = 0;
//                            //    //string command = "-20\n";
//                            //    //byte[] data = Encoding.UTF8.GetBytes(command);
//                            //    //TCP_SND_BYTE_List.AddRange(data);

//                            //    byte[] data = Encoding.UTF8.GetBytes("PING");
//                            //    stream.Write(data);
//                            //}
//                        }





//                    }

//                    //CommunicateAsync(client);
//                }
//            }
//            catch (SocketException ex)
//            {
//                TCP_CLIENT_CONNECTED = false;

//                myTextView.Append($"\nError connecting to server: {ex.Message}");
//                Thread.Sleep(1000);
//            }
//            catch (Exception ex)
//            {
//                TCP_CLIENT_CONNECTED = false;
//                myTextView.Append($"\nAn unexpected error occurred: {ex.Message}");
//            }
//            finally
//            {
//                // myTextView.Append("\nClient finished. Press any key to exit.");
//            }


//            return false;
//            */














//            /*
//            try
//            {
//                _client = new TcpClient();
//                _client.Connect(host, port);
//                _stream = _client.GetStream();

//                RunOnUiThread(() =>
//                {
//                    myTextView.Append("\nTCP Connection OK");
//                });

//                return true;

//            }
//            catch (Exception ex)
//            {
//                RunOnUiThread(() =>
//                {
//                    myTextView.Append("\nTCP Connection failed: " + ex.Message);
//                });

//                return false;
//            }
//            */
//        }

//        private bool TCP_Client_Listen()
//        {
//            /*
//            if (_stream == null)
//            {
//                return false;
//            }

//            try
//            {
//                byte[] buffer = new byte[10000];
//                int bytes = _stream.Read(buffer, 0, buffer.Length);

//                RunOnUiThread(() =>
//                {
//                    myTextView.Append("\nTCP Data RCV: " + Encoding.UTF8.GetString(buffer, 0, bytes));
//                });
//            }
//            catch (Exception ex)
//            {
//                RunOnUiThread(() =>
//                {
//                    myTextView.Append("\nTCP RCV failed: " + ex.Message);
//                });

//                return false;
//            }
//            */

//            return true;
//        }

//        private bool TCP_Client_SND(byte[] data)
//        {
//            TCP_SND_BYTE_List.AddRange(data);

//            /*
//            if (_stream == null)
//            {
//                return false;
//            }

//            try
//            {
//                _stream.Write(data, 0, data.Length);
//            }
//            catch (Exception ex)
//            {
//                RunOnUiThread(() =>
//                {
//                    myTextView.Append("\nTCP SND failed: " + ex.Message);
//                });

//                return false;
//            }

//            */
//            return true;
//        }




//        private void TCP_Thread()
//        {
//            //string TempStr = "";
//            //byte[] RCV_Bytes = null;
//            Thread_TCP_Step = 3;
//            Thread_TCP_Terminate = false;

//            bool OK = false;


//            List<byte> My_BYTE_List = new List<byte>();
//            My_BYTE_List.Clear();

//            while (!Thread_TCP_Terminate)
//            {
//                TCP_Client_Connect();















//                //    /*

//                //    OK = TCP_Client_Connect();

//                //    if (OK == false)
//                //    {
//                //        Thread.Sleep(1000);
//                //    }
//                //    else if (OK == true) 
//                //    {
//                //        RunOnUiThread(() =>
//                //        {
//                //            myTextView.Append("\nTCP Connection Start Listening");
//                //        });


//                //        while (!Thread_TCP_Terminate)
//                //        {
//                //            if (TCP_Client_Listen() == false)
//                //            {
//                //                break;
//                //            }

//                //        }
//                //    }
//                //    */



//                //}
//                //while (!Thread_TCP_Terminate)
//                //{
//                //    TCP_Client_Connect();

//                //    if (!Thread_TCP_Terminate)
//                //    {
//                //        Thread.Sleep(1000);
//                //    }
//            }
//        }

//        //=============================================================================================

//    }
//}



using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Content.PM;

namespace AndroidApp1;

[Activity(Label = "@string/app_name", MainLauncher = true, Exported = true)]
public class MainActivity : Activity
{
    private TextView? _textView;
    private BroadcastReceiver? _logReceiver;
    private bool _receiverRegistered;

    private const string PreferencesName = "bridge_preferences";
    private const string UserStoppedKey = "bridge_user_stopped";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);
        SetContentView(Resource.Layout.activity_main);

        _textView = FindViewById<TextView>(Resource.Id.myTextView);
        if (_textView != null)
        {
            _textView.MovementMethod = new Android.Text.Method.ScrollingMovementMethod();
            _textView.Text = "Ports:";
        }

        var findButton = FindViewById<Button>(Resource.Id.myButton);
        var disconnectButton = FindViewById<Button>(Resource.Id.Open_BTN);
        var clearButton = FindViewById<Button>(Resource.Id.Clear_BTN);

        if (findButton != null)
        {
            findButton.Click += (_, _) =>
            {
                GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                    ?.Edit()?.PutBoolean(UserStoppedKey, false)?.Apply();
                StartBridgeService();
                AppendLog("Find Ports - OPEN requested.");
            };
        }

        if (disconnectButton != null)
            disconnectButton.Click += (_, _) => StopBridgeService();

        if (clearButton != null)
            clearButton.Click += (_, _) => { if (_textView != null) _textView.Text = string.Empty; };

        RegisterLogReceiver();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
            CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 1001);
        }

        var preferences = GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        if (preferences?.GetBoolean(UserStoppedKey, false) != true)
            StartBridgeService();
    }

    private void StartBridgeService()
    {
        try
        {
            var intent = new Intent(this, typeof(DiagnosticBridgeService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                StartForegroundService(intent);
            else
                StartService(intent);
        }
        catch (Exception ex)
        {
            AppendLog("Could not start bridge service: " + ex.Message);
        }
    }

    private void StopBridgeService()
    {
        GetSharedPreferences(PreferencesName, FileCreationMode.Private)
            ?.Edit()?.PutBoolean(UserStoppedKey, true)?.Apply();
        StopService(new Intent(this, typeof(DiagnosticBridgeService)));
        AppendLog("Disconnect requested by user.");
    }

    private void RegisterLogReceiver()
    {
        _logReceiver = new BridgeLogReceiver(AppendLog);
        var filter = new IntentFilter(DiagnosticBridgeService.LogAction);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            RegisterReceiver(_logReceiver, filter, ReceiverFlags.NotExported);
        else
            RegisterReceiver(_logReceiver, filter);
        _receiverRegistered = true;
    }

    private void AppendLog(string text)
    {
        RunOnUiThread(() =>
        {
            if (_textView == null) return;
            _textView.Append("\n" + DateTime.Now.ToString("HH:mm:ss") + "  " + text);
            var layout = _textView.Layout;
            if (layout != null)
                _textView.ScrollTo(0, Math.Max(0, layout.GetLineTop(_textView.LineCount) - _textView.Height));
        });
    }

    protected override void OnDestroy()
    {
        // Do not stop the bridge here: Activity destruction is not an explicit Disconnect.
        if (_receiverRegistered && _logReceiver != null)
        {
            try { UnregisterReceiver(_logReceiver); } catch { }
            _receiverRegistered = false;
        }
        base.OnDestroy();
    }

    private sealed class BridgeLogReceiver : BroadcastReceiver
    {
        private readonly Action<string> _append;
        public BridgeLogReceiver(Action<string> append) => _append = append;

        public override void OnReceive(Context? context, Intent? intent)
        {
            var message = intent?.GetStringExtra(DiagnosticBridgeService.LogExtra);
            if (!string.IsNullOrWhiteSpace(message))
                _append(message);
        }
    }
}
