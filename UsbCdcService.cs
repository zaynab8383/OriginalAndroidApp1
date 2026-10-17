#if ANDROID
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Java.IO;
using System.Text;
//using System.Threading;

public class UsbCdcService
{


    private UsbManager _usbManager;
    private UsbDeviceConnection _connection;
    private UsbEndpoint _endpointIn;
    private UsbEndpoint _endpointOut;
    private UsbInterface _usbInterface;

    public UsbCdcService()
    {
        _usbManager = (UsbManager)Android.App.Application.Context
            .GetSystemService(Context.UsbService);
    }

    public UsbDevice FindCdcDevice()
    {
        foreach (var device in _usbManager.DeviceList.Values)
        {
            for (int i = 0; i < device.InterfaceCount; i++)
            {
                var intf = device.GetInterface(i);

                // CDC class = 2
                if (intf.InterfaceClass == UsbClass.Comm ||
                    intf.InterfaceClass == UsbClass.CdcData)
                {
                    return device;
                }
            }
        }
        return null;
    }






    public bool OpenDevice(UsbDevice device)
    {

        //private const string ACTION_USB_PERMISSION = "com.yourapp.USB_PERMISSION";
        //const string ACTION_USB_PERMISSION = "com.companyname.AndroidApp1.USB_PERMISSION";
        const string ACTION_USB_PERMISSION = "com.AndroidApp1.USB_PERMISSION";

        if (!_usbManager.HasPermission(device))
        {
            var permissionIntent = PendingIntent.GetBroadcast(
                Android.App.Application.Context,     // context
                0,                                   // request code
                new Intent(ACTION_USB_PERMISSION),   // intent
                                                     //PendingIntentFlags.Mutable           // flags (use .Immutable for Android < 12)
                PendingIntentFlags.Immutable //| PendingIntentFlags.UpdateCurrent
            );

            _usbManager.RequestPermission(device, permissionIntent);

            return false;
        }

        _connection = _usbManager.OpenDevice(device);

        for (int i = 0; i < device.InterfaceCount; i++)
        {
            var intf = device.GetInterface(i);

            if (intf.InterfaceClass == UsbClass.CdcData)
            {
                _usbInterface = intf;
                _connection.ClaimInterface(intf, true);

                for (int e = 0; e < intf.EndpointCount; e++)
                {
                    var ep = intf.GetEndpoint(e);
                    if (ep.Direction == UsbAddressing.In)
                        _endpointIn = ep;
                    else if (ep.Direction == UsbAddressing.Out)
                        _endpointOut = ep;
                }
            }
        }

        return _endpointIn != null && _endpointOut != null;
    }


    public void Write_Byte(Byte[] buffer, int Time_out)
    {
        _connection.BulkTransfer(_endpointOut, buffer, buffer.Length, Time_out);
    }

    public void Write_Str(string text, int Time_out)
    {
        var buffer = Encoding.UTF8.GetBytes(text);
        _connection.BulkTransfer(_endpointOut, buffer, buffer.Length, Time_out);
    }

    public byte[] Read_Byte(int Time_out)
    {
        byte[] buffer = new byte[2048];
        int length = _connection.BulkTransfer(_endpointIn, buffer, buffer.Length, Time_out);

        if (length == 0)
        {
            return null;
        }
        else if (length == -1)
        {
            return null;
        }



        byte[] ret = new byte[length];
        Array.Copy(buffer, ret, length);

        return ret;
    }


    public string Read_Str(int Time_out)
    {
        byte[] buffer = new byte[64];
        int length = _connection.BulkTransfer(_endpointIn, buffer, buffer.Length, Time_out);

        if (length > 0)
            //return Encoding.ASCII.GetString(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer, 0, length);

        return string.Empty;
    }

    public void Close()
    {
        _connection?.ReleaseInterface(_usbInterface);
        _connection?.Close();
    }
}
#endif
