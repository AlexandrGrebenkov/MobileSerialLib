using Android.Bluetooth;
using AndroidBluetoothLE.Bluetooth.Client;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MobileSerial_BLE;
using System.Threading.Tasks;

namespace MobileSerial_BLE.Droid
{
    public class MSL_BLE_Droid : IMSerial_BLE
    {
        BluetoothConnectionHandler _connectionHandler;
        DeviceWritingHandler _writingHandler;
        BluetoothGattCharacteristic _characteristic;
        BluetoothDevice device;

        BluetoothClient _bluetoothClient;
        BluetoothDeviceScanner _scanner;

        ObservableCollection<BluetoothDevice> _deviceList = new ObservableCollection<BluetoothDevice>();

        public BLE_Status Status { get; set; }

        byte[] RxData;
        //====================================
        IEnumerable<BluetoothGattService> GetServices()
        {
            var filterUuid = new[] {
                UUID.FromString("00001800-0000-1000-8000-00805F9B34FB"),
                UUID.FromString("00001801-0000-1000-8000-00805F9B34FB"),
                UUID.FromString("7905F431-B5CE-4E99-A40F-4B1E122D00D0") };
            var serviceList = _connectionHandler.GetServiceList().Where(s => filterUuid.All(uuid => !uuid.Equals(s.Uuid)));
            return serviceList;
        }

        BluetoothGattCharacteristic GetCharacteristic(IEnumerable<BluetoothGattService> serviceList)
        {
            var uuid = UUID.FromString("49535343-1e4d-4bd9-ba61-23c647249616");

            var service = serviceList.First(s => s.Characteristics.Any(ch => ch.Uuid.Equals(uuid)));
            return service.Characteristics.First(ch => ch.Uuid.Equals(uuid));
        }
        //====================================

        /// <summary>
        /// Инициализация Bluetooth-модуля
        /// </summary>
        /// <returns></returns>
        public BLE_Status BLE_Init()
        {
            _bluetoothClient = BluetoothClient.Instance;
            _bluetoothClient.Initialize();

            if (_bluetoothClient.Adapter == null) return BLE_Status.BT_NotAwailable;  //Bluetooth не поддерживается на этом устройстве
            if (_bluetoothClient.Adapter.IsEnabled == false) return BLE_Status.BT_IsSwitchOff;   //Bluetooth выключен
            if (_bluetoothClient.IsBLEEnabled == false) return BLE_Status.BLE_NotAwailable; //Bluetooth Low Energy не поддерживается на этом устройстве
            if (_bluetoothClient.IsInitialized == false) return BLE_Status.NotConnect;       //Bluetooth Уже инициализирован

            Status = BLE_Status.NotConnect;

            _scanner = new BluetoothDeviceScanner(_bluetoothClient.Adapter, (BluetoothDevice dev) =>
            {
                if (_deviceList.All(d => !d.Address.Equals(dev.Address, StringComparison.OrdinalIgnoreCase)))
                {
                    if (dev.Name != null)
                    {
                        _deviceList.Add(dev);
                        onDeviceFound?.Invoke(new BLE_Device_Info() { Name = dev.Name, Address = dev.Address });
                    }
                }
            });

            return BLE_Status.NotConnect;
        }

        public void Connect(Action<bool> action)
        {
            _connectionHandler = BluetoothClient.Instance.ConnectionHandler;
            if (_connectionHandler.IsConnected) return;

            var client = BluetoothClient.Instance;
            _connectionHandler.Connect(device, (ProfileState profileState) =>
            {
                switch (profileState)
                {
                    case ProfileState.Connected:
                    {//Подключились
                            _connectionHandler.DiscoverServices(status =>
                        {
                            if (status == GattStatus.Success)
                            {//Получен список сервисов
                                    var serviceList = GetServices();

                                _characteristic = GetCharacteristic(serviceList);
                                var notify = new DeviceNotifyingHandler(_connectionHandler.GattValue, GattClientObserver.Instance);

                                notify.Subscribe(_characteristic, (bool notifyStatus) =>
                                {
                                    if (notifyStatus == true)
                                    {//Нотификация пройдена
                                            _writingHandler = new DeviceWritingHandler(_connectionHandler.GattValue, GattClientObserver.Instance);
                                        _writingHandler.ReceivedReadResponce += _writingHandler_ReceivedReadResponce;
                                        Status = BLE_Status.Connect;
                                        action?.Invoke(true);
                                    }
                                    else
                                    {
                                            //_writingHandler.ReceivedReadResponce -= _writingHandler_ReceivedReadResponce;
                                            action?.Invoke(false);
                                    }
                                    notify.Dispose();
                                });

                            }
                            else
                                action?.Invoke(false);
                        });
                        break;
                    }
                    case ProfileState.Disconnected:
                    {
                        Status = BLE_Status.NotConnect;
                        action?.Invoke(false);
                        break;
                    }
                    default:
                    {
                        action?.Invoke(false);
                        break;
                    }
                }
            });

        }

        public void Disconnect()
        {
            if (_connectionHandler.IsConnected == true)
                _connectionHandler.DisconnectAsync();
        }

        /// <summary>
        /// Сюда попадаем при получении посылки
        /// </summary>
        /// <param name="data"></param>
        private void _writingHandler_ReceivedReadResponce(byte[] data)
        {
            Debug.WriteLine("Thread name is: {0}.", Thread.CurrentThread.Name);
            RxData = data;
            _execute?.BeginInvoke(data, (d) =>
            {

            }, null);//  Invoke(data);
        }


        Action<byte[]> _execute;

        public void RxCallback(Action<byte[]> execute)
        {
            _execute = execute;
        }

        /// <summary>
        /// Метод чтения
        /// </summary>
        /// <param name="timeout">Таймаут (в мс)</param>
        /// <returns>Возвращает принятый буфер</returns>
        public byte[] Read(uint timeout = 1000)
        {
            int t = 0;
            int period = 10;
            RxData = null;
            byte[] data = null; ;
            while (t < timeout)
            {
                if (RxData != null)
                {
                    data = new byte[RxData.Length];
                    for (int i = 0; i < RxData.Length; i++)
                    {
                        data[i] = RxData[i];
                    }
                    RxData = null;
                    break;
                }
                Thread.Sleep(period); // Task.Delay(period);
                t += period;
            }
            return data;
        }

        public async Task<byte[]> ReadAsync(uint timeout = 1000)
        {
            int t = 0;
            int period = 10;
            RxData = null;
            byte[] data = null; ;
            while (t < timeout)
            {
                if (RxData != null)
                {
                    data = new byte[RxData.Length];
                    for (int i = 0; i < RxData.Length; i++)
                    {
                        data[i] = RxData[i];
                    }
                    RxData = null;
                    break;
                }
                await Task.Delay(period);
                t += period;
            }
            return data;
        }

        public void Write(byte[] TxBuff, int timeout = 1000)
        {
            _writingHandler.Write(TxBuff, _characteristic, true);
            RxData = null;
        }

        private Action<BLE_Device_Info> onDeviceFound;
        public void StartScan(Action<BLE_Device_Info> execute)
        {
            if ((Status != BLE_Status.BT_NotAwailable) &&
                (Status != BLE_Status.BT_IsSwitchOff) &&
                (Status != BLE_Status.BLE_NotAwailable))
            {
                onDeviceFound = execute;
                _deviceList.Clear();
                _scanner.StartScan();
            }
        }

        public void StopScan()
        {
            if ((Status != BLE_Status.BT_NotAwailable) &&
                (Status != BLE_Status.BT_IsSwitchOff) &&
                (Status != BLE_Status.BLE_NotAwailable))
            {
                _scanner.StopScan();
            }
        }

        public void SelectDeviceByName(string name)
        {
            for (int i = 0; i < _deviceList.Count; i++)
            {
                if (_deviceList[i].Name == name)
                {
                    device = _deviceList[i];
                    break;
                }
            }
        }

        public void SelectDeviceByAddress(string address)
        {
            for (int i = 0; i < _deviceList.Count; i++)
            {
                if (_deviceList[i].Address == address)
                {
                    device = _deviceList[i];
                    return;
                }
            }

            if (BluetoothAdapter.CheckBluetoothAddress(address))
            {
                BluetoothAdapter adapter = BluetoothAdapter.DefaultAdapter;
                device = adapter.GetRemoteDevice(address);
            }
        }

       
    }
}