using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.Hardware.Usb;
using MobileSerialLib.USB;

namespace MobileSerial_BLE.Droid.USB
{
    public class MSL_USB_Droid : IMSerial_USB
    {
        void Log(string message) =>
            System.Diagnostics.Debug.WriteLine(message);

        int VENDOR_ID = 0x483;
        int PRODUCT_ID = 0x100B;

        Context _ContextWrapper;

        UsbManager usbManager;
        UsbDeviceConnection deviceConnection;

        UsbEndpoint writer;
        UsbEndpoint reader;

        Action<bool> ConnectionAction;

        public MSL_USB_Droid(Context ContextWrapper, int vid, int pid)
        {
            VENDOR_ID = vid;
            PRODUCT_ID = pid;
            _ContextWrapper = ContextWrapper;
        }

        public void Connect(Action<bool> action)
        {
            ConnectionAction = action;
            // Get a usbManager that can access all of the devices
            usbManager = (UsbManager)_ContextWrapper.GetSystemService(Context.UsbService);
            if (usbManager == null) return;//На случай, если телефон без USB. Хотя, не знаю на сколько это реально
            // Ищем наш девайс
            var matchingDevice = usbManager.DeviceList.FirstOrDefault(item =>
                                                                      item.Value.VendorId == VENDOR_ID &&
                                                                      item.Value.ProductId == PRODUCT_ID);
            // DeviceList is a dictionary with the port as the key, so pull out the device you want.  I save the port too
            var usbPort = matchingDevice.Key;
            var usbDevice = matchingDevice.Value;

            Disconnect();

            //Если устройство подключено к шине
            if (usbDevice == null)
            {
                Log("Device is null");
                return;
            }

            Log($"Name: {usbDevice.DeviceName}");
            // Запрос разрешения. Если запускать без PendingIntent, то крашится GUI (даже в AndroidStudio)
            /*if (!usbManager.HasPermission(usbDevice))
                usbManager.RequestPermission(usbDevice, null);*/
            Log("Открываем соединение...");
            //Если было подключение до этого
            
            try
            {
                // Открываем соединение
                deviceConnection = usbManager.OpenDevice(usbDevice);
                //Запрашиваем интерфейс, в котором должно быть две конечные точки
                var usbInterface = usbDevice.GetInterface(0);

                deviceConnection.ClaimInterface(usbInterface, true);
                Log($"EndpointCount = {usbInterface.EndpointCount}");
                Log("GetEndpoint(0)");
                writer = usbInterface.GetEndpoint(0);
                Log("GetEndpoint(1)");
                reader = usbInterface.GetEndpoint(1);
                Log("OK!");
                ConnectionAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                Log($"Exception: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            // Close the connection
            deviceConnection?.Close();
            deviceConnection?.Dispose();
            deviceConnection = null;
            ConnectionAction?.Invoke(false);
        }

        public byte[] Read(uint timeout = 1000)
        {
            byte[] RxBuff = new byte[256];
            var bc = deviceConnection.BulkTransfer(reader, RxBuff, RxBuff.Length, (int)timeout);
            if (bc <= 0) return null;
            byte[] res = new byte[bc];
            Buffer.BlockCopy(RxBuff, 0, res, 0, bc);
            return res;
        }

        public async Task<byte[]> ReadAsync(uint timeout = 1000)
        {
            byte[] RxBuff = new byte[256];
            var bc = await deviceConnection.BulkTransferAsync(reader, RxBuff, RxBuff.Length, (int)timeout);
            if (bc <= 0) return null;
            byte[] res = new byte[bc];
            Buffer.BlockCopy(RxBuff, 0, res, 0, bc);
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
    }
}