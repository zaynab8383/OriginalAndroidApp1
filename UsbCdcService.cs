//#if ANDROID
//using Android.App;
//using Android.Content;
//using Android.Hardware.Usb;
//using Java.IO;
//using System.Text;
////using System.Threading;

//public class UsbCdcService
//{


//    private UsbManager _usbManager;
//    private UsbDeviceConnection _connection;
//    private UsbEndpoint _endpointIn;
//    private UsbEndpoint _endpointOut;
//    private UsbInterface _usbInterface;

//    public UsbCdcService()
//    {
//        _usbManager = (UsbManager)Android.App.Application.Context
//            .GetSystemService(Context.UsbService);
//    }

//    public UsbDevice FindCdcDevice()
//    {
//        foreach (var device in _usbManager.DeviceList.Values)
//        {
//            for (int i = 0; i < device.InterfaceCount; i++)
//            {
//                var intf = device.GetInterface(i);

//                // CDC class = 2
//                if (intf.InterfaceClass == UsbClass.Comm ||
//                    intf.InterfaceClass == UsbClass.CdcData)
//                {
//                    return device;
//                }
//            }
//        }
//        return null;
//    }






//    public bool OpenDevice(UsbDevice device)
//    {

//        //private const string ACTION_USB_PERMISSION = "com.yourapp.USB_PERMISSION";
//        //const string ACTION_USB_PERMISSION = "com.companyname.AndroidApp1.USB_PERMISSION";
//        const string ACTION_USB_PERMISSION = "com.AndroidApp1.USB_PERMISSION";

//        if (!_usbManager.HasPermission(device))
//        {
//            var permissionIntent = PendingIntent.GetBroadcast(
//                Android.App.Application.Context,     // context
//                0,                                   // request code
//                new Intent(ACTION_USB_PERMISSION),   // intent
//                                                     //PendingIntentFlags.Mutable           // flags (use .Immutable for Android < 12)
//                PendingIntentFlags.Immutable //| PendingIntentFlags.UpdateCurrent
//            );

//            _usbManager.RequestPermission(device, permissionIntent);

//            return false;
//        }

//        _connection = _usbManager.OpenDevice(device);

//        for (int i = 0; i < device.InterfaceCount; i++)
//        {
//            var intf = device.GetInterface(i);

//            if (intf.InterfaceClass == UsbClass.CdcData)
//            {
//                _usbInterface = intf;
//                _connection.ClaimInterface(intf, true);

//                for (int e = 0; e < intf.EndpointCount; e++)
//                {
//                    var ep = intf.GetEndpoint(e);
//                    if (ep.Direction == UsbAddressing.In)
//                        _endpointIn = ep;
//                    else if (ep.Direction == UsbAddressing.Out)
//                        _endpointOut = ep;
//                }
//            }
//        }

//        return _endpointIn != null && _endpointOut != null;
//    }


//    public void Write_Byte(Byte[] buffer, int Time_out)
//    {
//        _connection.BulkTransfer(_endpointOut, buffer, buffer.Length, Time_out);
//    }

//    public void Write_Str(string text, int Time_out)
//    {
//        var buffer = Encoding.UTF8.GetBytes(text);
//        _connection.BulkTransfer(_endpointOut, buffer, buffer.Length, Time_out);
//    }

//    public byte[] Read_Byte(int Time_out)
//    {
//        byte[] buffer = new byte[2048];
//        int length = _connection.BulkTransfer(_endpointIn, buffer, buffer.Length, Time_out);

//        if (length == 0)
//        {
//            return null;
//        }
//        else if (length == -1)
//        {
//            return null;
//        }



//        byte[] ret = new byte[length];
//        Array.Copy(buffer, ret, length);

//        return ret;
//    }


//    public string Read_Str(int Time_out)
//    {
//        byte[] buffer = new byte[64];
//        int length = _connection.BulkTransfer(_endpointIn, buffer, buffer.Length, Time_out);

//        if (length > 0)
//            //return Encoding.ASCII.GetString(buffer, 0, length);
//            return Encoding.UTF8.GetString(buffer, 0, length);

//        return string.Empty;
//    }

//    public void Close()
//    {
//        _connection?.ReleaseInterface(_usbInterface);
//        _connection?.Close();
//    }
//}
//#endif

#if ANDROID
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using System.Text;

public class UsbCdcService
{
    public const string PermissionAction = "com.companyname.AndroidApp1.USB_PERMISSION";

    private readonly UsbManager _usbManager;
    private UsbDeviceConnection? _connection;
    private UsbEndpoint? _endpointIn;
    private UsbEndpoint? _endpointOut;
    private UsbInterface? _usbInterface;

    public UsbCdcService()
    {
        _usbManager = (UsbManager)Android.App.Application.Context
            .GetSystemService(Context.UsbService)!;
    }

    public UsbDevice? FindCdcDevice()
    {
        foreach (var device in _usbManager.DeviceList.Values)
        {
            for (int i = 0; i < device.InterfaceCount; i++)
            {
                var intf = device.GetInterface(i);
                if (intf.InterfaceClass == UsbClass.Comm || intf.InterfaceClass == UsbClass.CdcData)
                    return device;
            }
        }
        return null;
    }

    public bool HasPermission(UsbDevice device) => _usbManager.HasPermission(device);

    public void RequestPermission(UsbDevice device)
    {
        if (_usbManager.HasPermission(device)) return;

        var context = Android.App.Application.Context;
        var permissionIntent = PendingIntent.GetBroadcast(
            context,
            0,
            new Intent(PermissionAction).SetPackage(context.PackageName),
            PendingIntentFlags.Mutable | PendingIntentFlags.UpdateCurrent);

        _usbManager.RequestPermission(device, permissionIntent);
    }

    public bool OpenDevice(UsbDevice device)
    {
        if (!_usbManager.HasPermission(device))
        {
            RequestPermission(device);
            return false;
        }

        Close();
        _connection = _usbManager.OpenDevice(device);
        if (_connection == null) return false;

        for (int i = 0; i < device.InterfaceCount; i++)
        {
            var intf = device.GetInterface(i);
            if (intf.InterfaceClass != UsbClass.CdcData) continue;
            if (!_connection.ClaimInterface(intf, true)) continue;

            _usbInterface = intf;
            for (int e = 0; e < intf.EndpointCount; e++)
            {
                var endpoint = intf.GetEndpoint(e);
                if (endpoint.Direction == UsbAddressing.In) _endpointIn = endpoint;
                else if (endpoint.Direction == UsbAddressing.Out) _endpointOut = endpoint;
            }

            if (_endpointIn != null && _endpointOut != null) return true;
            try { _connection.ReleaseInterface(intf); } catch { }
            _usbInterface = null;
            _endpointIn = null;
            _endpointOut = null;
        }

        Close();
        return false;
    }

    public void Write_Byte(byte[] buffer, int Time_out)
    {
        if (_connection == null || _endpointOut == null || buffer == null || buffer.Length == 0)
            return;
        _connection.BulkTransfer(_endpointOut, buffer, buffer.Length, Time_out);
    }

    public void Write_Str(string text, int Time_out) => Write_Byte(Encoding.UTF8.GetBytes(text), Time_out);

    public byte[]? Read_Byte(int Time_out)
    {
        if (_connection == null || _endpointIn == null) return null;
        byte[] buffer = new byte[2048];
        int length = _connection.BulkTransfer(_endpointIn, buffer, buffer.Length, Time_out);
        if (length <= 0) return null;
        byte[] result = new byte[length];
        Array.Copy(buffer, result, length);
        return result;
    }

    public string Read_Str(int Time_out)
    {
        var bytes = Read_Byte(Time_out);
        return bytes == null ? string.Empty : Encoding.UTF8.GetString(bytes);
    }

    public void Close()
    {
        try
        {
            if (_connection != null && _usbInterface != null)
                _connection.ReleaseInterface(_usbInterface);
        }
        catch { }
        try { _connection?.Close(); } catch { }
        _connection = null;
        _endpointIn = null;
        _endpointOut = null;
        _usbInterface = null;
    }
}
#endif

