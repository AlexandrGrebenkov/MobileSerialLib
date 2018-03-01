using MobileSerialLib.BT_RFCOMM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MobileSerial.UWP.BT_RFCOMM
{
    public class BT_RFCOMM_UWP : IMSerial_BT_RFCOMM
    {
        List<string> DeviceNamesList;
        DeviceInformationCollection DeviceList;

        StreamSocket _socket;
        BluetoothDevice BT_Device;

        bool conectionStatus = false;

        public BT_RFCOMM_UWP()
        {
            Init();
        }

        async void Init()
        {
            DeviceList = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));
            if (DeviceList.Count != 0)
            {
                DeviceNamesList = new List<string>();
                for (int i = 0; i < DeviceList.Count; i++)
                {
                    DeviceNamesList.Add(DeviceList[i].Name);
                }
            }
        }

        public async void Connect(Action<bool> action)
        {
            string DeviceName = "";
            try
            {
                //получаем все сопряжённые устройства RFCOMM (Dev B)                
                var device = DeviceList.FirstOrDefault(x => x.Name == DeviceName);
                //BT_Device = await BluetoothDevice.FromIdAsync(device.Id);

                var _service = await RfcommDeviceService.FromIdAsync(device.Id);
                _socket = new StreamSocket();
                await _socket.ConnectAsync(
                      _service.ConnectionHostName,
                      _service.ConnectionServiceName,
                      SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                conectionStatus = true;
            }
            catch (Exception e)
            {
                conectionStatus = false;
            }
        }

        public void Disconnect()
        {
            _socket?.Dispose();
            _socket = null;
            conectionStatus = false;
        }

        public byte[] Read(uint timeout = 1000)
        {
            throw new NotImplementedException();
        }

        public async Task<byte[]> ReadAsync(uint timeout = 1000)
        {
            byte[] data = null;

            try
            {
                using (var reader = new DataReader(_socket.InputStream))
                {
                    var buffer = reader.ReadBuffer(5);

                    var tmp = buffer.ToArray();
                    data = new byte[5];
                    for (int i = 0; i < 5; i++)
                    {
                        data[i] = tmp[i];
                    }
                }
            }
            catch (Exception ex)
            {
                Disconnect();
            }
            return data;
        }

        public void RxCallback(Action<byte[]> execute)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] TxBuff, int timeout = 1000)
        {
            throw new NotImplementedException();
        }
    }
}
