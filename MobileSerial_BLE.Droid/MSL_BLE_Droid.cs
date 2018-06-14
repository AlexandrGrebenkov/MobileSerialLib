using Android.Bluetooth;
using AndroidBluetoothLE.Bluetooth.Client;
using Java.Util;
using MobileSerialLib;
using MobileSerialLib.BLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MobileSerial_BLE.Droid
{
    public class MSL_BLE_Droid : IMSerial_BLE
    {
        DeviceWritingHandler _writingHandler;
        BluetoothGattCharacteristic _characteristic;
        BluetoothDevice device;

        BluetoothDeviceScanner _scanner;
        List<BluetoothDevice> DeviceList = new List<BluetoothDevice>();

        public BLE_Status Status { get; set; }

        /// <summary>
        /// Инициализация Bluetooth-модуля
        /// </summary>
        /// <returns></returns>
        public BLE_Status BLE_Init()
        {
            BluetoothClient _bluetoothClient = BluetoothClient.Instance;
            _bluetoothClient.Initialize();

            if (_bluetoothClient.Adapter == null) return BLE_Status.BT_NotAwailable;  //Bluetooth не поддерживается на этом устройстве
            if (_bluetoothClient.Adapter.IsEnabled == false) return BLE_Status.BT_IsSwitchOff;   //Bluetooth выключен
            if (_bluetoothClient.IsBLEEnabled == false) return BLE_Status.BLE_NotAwailable; //Bluetooth Low Energy не поддерживается на этом устройстве
            if (_bluetoothClient.IsInitialized == false) return BLE_Status.NotConnect;       //Bluetooth Уже инициализирован

            Status = BLE_Status.NotConnect;

            _scanner = new BluetoothDeviceScanner(_bluetoothClient.Adapter, (BluetoothDevice dev) =>
            {
                if (DeviceList.All(d => !d.Address.Equals(dev.Address, StringComparison.OrdinalIgnoreCase)))
                {
                    if (dev.Name != null)
                    {
                        DeviceList.Add(dev);
                        onDeviceFound?.Invoke(new BLE_Device_Info() { Name = dev.Name, Address = dev.Address });
                    }
                }
            });

            return BLE_Status.NotConnect;
        }

        #region Connect/Disconnect
        BluetoothConnectionHandler _connectionHandler;

        /// <summary>
        /// Подключение к девайсу
        /// </summary>
        /// <param name="action"></param>
        public void Connect(Action<bool> action)
        {
            //Callback hell!
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
                                _characteristic = GetCharacteristic(GetServices());
                                var notify = new DeviceNotifyingHandler(_connectionHandler.GattValue, GattClientObserver.Instance);

                                notify.Subscribe(_characteristic, (bool notifyStatus) =>
                                {
                                    if (notifyStatus == true)
                                    {//Нотификация пройдена
                                        _writingHandler = new DeviceWritingHandler(_connectionHandler.GattValue, GattClientObserver.Instance);
                                        _writingHandler.ClearAllReadEvents();
                                        _writingHandler.ReceivedReadResponce += _writingHandler_ReceivedReadResponce;
                                        Status = BLE_Status.Connect;
                                        RxPacks.Clear();
                                        action?.Invoke(true);
                                    }
                                    else
                                    {//Если нотификация не пройдена, то делать нам тут нечего и мы отключаемся от девайса
                                        action?.Invoke(false);
                                    }
                                    notify.Dispose();//при любом результате выходим так
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


            //====================================
            IEnumerable<BluetoothGattService> GetServices()
            {
                var filterUuid = new[] {
                UUID.FromString("00001800-0000-1000-8000-00805F9B34FB"),
                UUID.FromString("00001801-0000-1000-8000-00805F9B34FB"),
                UUID.FromString("7905F431-B5CE-4E99-A40F-4B1E122D00D0") };
                return _connectionHandler?.GetServiceList().Where(s => filterUuid.All(uuid => !uuid.Equals(s.Uuid)));
            }

            BluetoothGattCharacteristic GetCharacteristic(IEnumerable<BluetoothGattService> serviceList)
            {
                var uuid = UUID.FromString("49535343-1e4d-4bd9-ba61-23c647249616");

                var service = serviceList.First(s => s.Characteristics.Any(ch => ch.Uuid.Equals(uuid)));
                return service.Characteristics.First(ch => ch.Uuid.Equals(uuid));
            }
            //====================================
        }

        /// <summary>
        /// Отключение
        /// </summary>
        public void Disconnect()
        {
            _connectionHandler.DisconnectAsync();
            _writingHandler.ClearAllReadEvents();
            /*if (_writingHandler != null)
                _writingHandler.ReceivedReadResponce -= _writingHandler_ReceivedReadResponce;*/
            RxPacks.Clear();
        }

        public void Close()
        {
            _connectionHandler.Close();
            _writingHandler.ClearAllReadEvents();
            /*if (_writingHandler != null)
                _writingHandler.ReceivedReadResponce -= _writingHandler_ReceivedReadResponce;*/
            RxPacks.Clear();
        }
        #endregion

        #region Запись/Чтение
        /// <summary>Буфер приёма</summary>
        byte[] RxData;

        List<RxData> RxPacks = new List<RxData>();

        public void Write(byte[] TxBuff, int timeout = 1000)
        {
            _writingHandler.Write(TxBuff, _characteristic, true);
        }

        /// <summary>
        /// Сюда попадаем при получении посылки
        /// </summary>
        /// <param name="data"></param>
        private void _writingHandler_ReceivedReadResponce(byte[] data)
        {
            try
            {
                RxPacks.Add(new RxData { RxPack = data, Date = DateTime.Now });
                RxData = data;
                foreach (var item in RxPacks)
                {
                    if (DateTime.Now - item.Date > TimeSpan.FromSeconds(3))
                        RxPacks.Remove(item);
                }

                _execute?.Invoke(data);
            }
            catch (Exception ex)
            {

            }
        }

        Action<byte[]> _execute;

        public void RxCallback(Action<byte[]> execute)
        {
            _execute = execute;
        }

        /// <summary>
        /// Метод чтения (Блокирует UI)
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

        /// <summary>
        /// Асинхронный метод чтения
        /// </summary>
        /// <param name="timeout">Таймаут (в мс)</param>
        /// <returns>Возвращает принятый буфер</returns>
        public async Task<byte[]> ReadAsync(uint timeout = 1000)
        {
            int t = 0;
            int period = 10;
            //RxData = null;
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

        public async Task<byte[]> ReadAsync(Func<byte[], bool> predicate, uint timeout = 1000)
        {
            int t = 0;
            int period = 10;

            byte[] result = null;
            while (t < timeout)
            {
                if (RxPacks.Count != 0)
                {
                    for (int i = RxPacks.Count - 1; i >= 0; i--)
                    {
                        var item = RxPacks[i];
                        if (predicate(item.RxPack))
                        {
                            result = item.RxPack;
                            FlushRxPool(predicate);
                            return result;
                        }
                    }
                }
                await Task.Delay(period);
                t += period;
            }

            return result;
        }

        void FlushRxPool(Func<byte[], bool> predicate)
        {//TODO: Не всегда корректно работает
            try
            {
                lock (this)
                {
                    foreach (var item in RxPacks)
                    {
                        if (predicate(item.RxPack))
                            RxPacks.Remove(item);
                    }
                }
            }
            catch { }
        }

        public List<RxData> GetList()
        {
            return RxPacks;
        }
        #endregion

        #region Сканирование/Выбор
        private Action<BLE_Device_Info> onDeviceFound;
        public void StartScan(Action<BLE_Device_Info> execute)
        {
            if ((Status != BLE_Status.BT_NotAwailable) &&
                (Status != BLE_Status.BT_IsSwitchOff) &&
                (Status != BLE_Status.BLE_NotAwailable))
            {
                onDeviceFound = execute;
                DeviceList.Clear();//Очистка списка устройств
                _scanner?.StartScan();//Запуск поиска
            }
        }

        public void StopScan() => _scanner?.StopScan();

        /// <summary>
        /// Выбор устройства по его имени
        /// </summary>
        /// <param name="address">Имя устройства</param>
        public void SelectDeviceByName(string name)
        {
            device = DeviceList.FirstOrDefault(d => d.Name == name);
        }

        /// <summary>
        /// Выбор устройства по его MAC-адресу
        /// </summary>
        /// <param name="address">MAC-Адрес</param>
        public void SelectDeviceByAddress(string address)
        {
            //Сначала ищем прибор в списке найденых
            device = DeviceList.FirstOrDefault(d => d.Address == address);
            if (device != null) return;

            //Если не нашли, то создаём новый BluetoothDevice по MAC-адресу
            if (BluetoothAdapter.CheckBluetoothAddress(address))
            {
                device = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(address);
                DeviceList.Add(device);
            }
            else
                throw new ArgumentException("MAC-Адрес не валидный", nameof(address));
        }
        #endregion


    }

}