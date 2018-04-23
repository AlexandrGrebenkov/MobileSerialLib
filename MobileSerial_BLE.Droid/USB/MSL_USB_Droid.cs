using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Java.Nio;
using MobileSerialLib;
using MobileSerialLib.USB;

namespace MobileSerial_BLE.Droid.USB
{
    public class MSL_USB_Droid : IMSerial_USB
    {
        private const int RequestPermissionCode = 20;
        private static String ACTION_USB_PERMISSION = "com.android.example.USB_PERMISSION";

        void Log(string message) =>
            System.Diagnostics.Debug.WriteLine(message);

        int VENDOR_ID;
        int PRODUCT_ID;

        Context _Context;

        UsbManager usbManager;
        UsbDeviceConnection deviceConnection;

        UsbEndpoint writer;
        UsbEndpoint reader;

        UsbRequest usbRequest;

        PendingIntent mPermissionIntent;
        UsbPermissionReciever usbReciever;

        Action<bool> ConnectionChanged;

        public MSL_USB_Droid(Context context, int vid, int pid)
        {
            VENDOR_ID = vid;
            PRODUCT_ID = pid;
            _Context = context;


        }

        public void Connect(Action<bool> action)
        {
            ConnectionChanged = action;
            // Get a usbManager that can access all of the devices
            usbManager = (UsbManager)_Context.GetSystemService(Context.UsbService);

            // Ищем наш девайс
            var matchingDevice = usbManager.DeviceList.FirstOrDefault(
                item => item.Value.VendorId == VENDOR_ID &&
                        item.Value.ProductId == PRODUCT_ID);
            // DeviceList is a dictionary with the port as the key, so pull out the device you want.  I save the port too
            var usbPort = matchingDevice.Key;
            var usbDevice = matchingDevice.Value;

            //Если устройство подключено к шине
            if (usbDevice == null)
            {
                Log("Device is null");
                ConnectionChanged?.Invoke(false);
                return;
            }

            Log($"Name: {usbDevice.DeviceName}");

            if (!usbManager.HasPermission(usbDevice))
            {
                #region Для запроса пермиссии 
                usbReciever = new UsbPermissionReciever(() => _Context.UnregisterReceiver(usbReciever));
                _Context.RegisterReceiver(usbReciever, new IntentFilter(ACTION_USB_PERMISSION));

                mPermissionIntent = PendingIntent.GetBroadcast(_Context, RequestPermissionCode, new Intent(ACTION_USB_PERMISSION), 0);
                usbReciever.InitUSB = () => InitUSB();
                #endregion

                usbManager.RequestPermission(usbDevice, mPermissionIntent);
            }
            else
                InitUSB();

            void InitUSB()
            {
                Log("Открываем соединение...");
                // Открываем соединение
                deviceConnection = null;
                try
                {
                    deviceConnection = usbManager.OpenDevice(usbDevice);
                    if (deviceConnection == null)
                    {
                        Log("Коннект не открылся!");
                        ConnectionChanged?.Invoke(false);
                        return;
                    }
                    Log($"InterfaceCount = {usbDevice.InterfaceCount}");
                    Log("GetInterface(0)");
                    var usbInterface = usbDevice.GetInterface(0);

                    deviceConnection.ClaimInterface(usbInterface, true);
                    Log($"EndpointCount = {usbInterface.EndpointCount}");
                    Log("GetEndpoint(0)");
                    writer = usbInterface.GetEndpoint(0);
                    Log("GetEndpoint(1)");
                    reader = usbInterface.GetEndpoint(1);
                    Log("OK!");

                    ConnectionChanged?.Invoke(true);
                }
                catch (Exception ex)
                {
                    Log($"Exception: {ex.Message}");
                    ConnectionChanged?.Invoke(false);
                }
            }
        }

        public void Disconnect()
        {
            // Close the connection
            deviceConnection?.Close();
            deviceConnection?.Dispose();
            deviceConnection = null;
            ConnectionChanged?.Invoke(false);
        }

        public byte[] Read(uint timeout = 1000)
        {
            byte[] RxBuff = new byte[256];
            var bc = deviceConnection.BulkTransfer(reader, RxBuff, RxBuff.Length, (int)timeout);
            if (bc <= 0) return null;
            byte[] res = new byte[bc];
            System.Buffer.BlockCopy(RxBuff, 0, res, 0, bc);
            return res;
        }

        public async Task<byte[]> ReadAsync(uint timeout = 1000)
        {
            byte[] RxBuff = new byte[256];
            var bc = await deviceConnection.BulkTransferAsync(reader, RxBuff, RxBuff.Length, (int)timeout);
            if (bc <= 0) return null;
            byte[] res = new byte[bc];
            System.Buffer.BlockCopy(RxBuff, 0, res, 0, bc);
            return res;
        }

        public void RxCallback(Action<byte[]> execute)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] TxBuff, int timeout = 1000)
        {
            deviceConnection.BulkTransfer(writer, TxBuff, TxBuff.Length, timeout);
        }

        public Task<byte[]> ReadAsync(Func<byte[], bool> predicate, uint timeout = 1000)
        {
            throw new NotImplementedException();
        }

        public List<byte[]> GetList()
        {
            throw new NotImplementedException();
        }

        List<RxData> IMSerial.GetList()
        {
            throw new NotImplementedException();
        }

        class UsbPermissionReciever : BroadcastReceiver
        {
            public Action InitUSB;
            Action Unregister;
            public override void OnReceive(Context context, Intent intent)
            {
                String action = intent.Action;
                if (ACTION_USB_PERMISSION.Equals(action))
                {
                    lock (this)
                    {
                        UsbDevice device = (UsbDevice)intent
                                .GetParcelableExtra(UsbManager.ExtraDevice);

                        if (intent.GetBooleanExtra(
                                UsbManager.ExtraPermissionGranted, false))
                        {
                            if (device != null)
                            {
                                InitUSB?.Invoke();
                            }
                        }
                        else
                        {

                        }
                    }
                }
                Unregister?.Invoke();
            }

            public UsbPermissionReciever(Action unregister)
            {
                Unregister = unregister;
            }
        }

    }
}