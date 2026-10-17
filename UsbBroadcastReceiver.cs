using Android.Content;
using Android.Hardware.Usb;
using Android.Widget;


[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(
    new[] {
    UsbManager.ActionUsbDeviceAttached,
    UsbManager.ActionUsbDeviceDetached
},
 Categories = new[] { Intent.CategoryDefault }

)]
public class UsbBroadcastReceiver : BroadcastReceiver
{
    public int VID, PID;
    public bool USB_Connected = false;


    public override void OnReceive(Context context, Intent intent)
    {
        string action = intent.Action;

        if (action == UsbManager.ActionUsbDeviceAttached)
        {
            UsbDevice device = (UsbDevice)intent.GetParcelableExtra(UsbManager.ExtraDevice);
            USB_Connected = true;

            if (device != null)
            {
                VID = device.VendorId;
                PID = device.ProductId;

                USB_Connected = true;

                //Toast.MakeText(context,
                //        $"USB Connected VID:{device.VendorId} PID:{device.ProductId}",
                //        ToastLength.Long).Show();
            }
            else
            {
                VID = -1;
                PID = -1;

                //Toast.MakeText(context, "USB device connected", ToastLength.Short).Show();
            }
        }

        if (action == UsbManager.ActionUsbDeviceDetached)
        {
            USB_Connected = false;
            //Toast.MakeText(context, "USB device disconnected", ToastLength.Short).Show();
        }
    }
}
