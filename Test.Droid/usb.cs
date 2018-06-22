using System;
using Android.App;
using Android.OS;
using Android.Widget;
using MobileSerial_BLE.Droid.USB;

namespace Test.Droid
{
    [Activity(Label = "usb", Theme = "@android:style/Theme.Material.Light")]
    public class usb : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.usb);

            var btnConnect = FindViewById<Button>(Resource.Id.btnUSBConnect);
            var btnWrite = FindViewById<Button>(Resource.Id.btnUSBWrite);
            var Output = FindViewById<TextView>(Resource.Id.tvUSBOutput);
            var btnDisconnect = FindViewById<Button>(Resource.Id.btnUSBDisconnect);

            MSL_USB_Droid USB = new MSL_USB_Droid(this, this, 0x483, 0x130);

            btnConnect.Click += (sender, e) =>
            {
                Log("Start connecting...");
                USB.Connect(status =>
                {
                    Log($"Connection changed. Status: {status}");
                });
            };

            btnDisconnect.Click += (sender, e) =>
            {
                USB.Disconnect();
                Log("Disconnect");
            };

            btnWrite.Click += (sender, e) =>
            {
                try
                {
                    var tx = new byte[] { 6, 10, 0, 0, 0 };
                    string t = string.Join(", ", tx);
                    Log($"Write: {t}");
                    USB.Write(tx);
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }
            };

            void Log(string message)
            {
                Output.Text += $"{DateTime.Now.ToLongTimeString()}: {message} \r\n";
            }
        }
    }
}