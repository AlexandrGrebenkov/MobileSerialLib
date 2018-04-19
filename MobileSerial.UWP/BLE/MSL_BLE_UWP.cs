using MobileSerialLib.BLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace MobileSerial.UWP.BLE
{
    public class MSL_BLE_UWP : IMSerial_BLE
    {
        public BLE_Status Status
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public BLE_Status BLE_Init()
        {
            throw new NotImplementedException();
        }

        public void Connect(Action<bool> action)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public byte[] Read(uint timeout = 1000)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ReadAsync(uint timeout = 1000)
        {
            throw new NotImplementedException();
        }

        public void RxCallback(Action<byte[]> execute)
        {
            throw new NotImplementedException();
        }

        public void SelectDeviceByAddress(string address)
        {
            throw new NotImplementedException();
        }

        public void SelectDeviceByName(string name)
        {
            throw new NotImplementedException();
        }

        public void StartScan(Action<BLE_Device_Info> execute)
        {
            DevicePicker picker = new DevicePicker();
            picker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));
            picker.Filter.SupportedDeviceSelectors.Add(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
            picker.Show(new Rect());
        }

        public void StopScan()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] TxBuff, int timeout = 1000)
        {
            throw new NotImplementedException();
        }
    }
}
