using Android.Bluetooth;

namespace AndroidBluetoothLE.Bluetooth.Client
{
    public delegate void ServicesDiscoveredEventHandler(BluetoothGatt gatt, GattStatus status);
    public delegate void ConnectionStateChangedEventHandler(BluetoothGatt gatt, GattStatus status, ProfileState newState);
    public delegate void CharacteristicValueChangedEventHandler(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic);
    public delegate void CharacteristicWrittenEventHandler(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus statuc);
    public delegate void DescriptorWrittenEventHandler(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status);
    public delegate void CharacteristicReadEventHandler(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status);


    public class GattClientObserver : BluetoothGattCallback
    {
        private static GattClientObserver _instance;

        public static GattClientObserver Instance
        {
            get { return _instance ?? (_instance = new GattClientObserver()); }
        }

        public event ServicesDiscoveredEventHandler ServicesDiscovered;
        public event ConnectionStateChangedEventHandler ConnectionStateChanged;
        public event CharacteristicValueChangedEventHandler CharacteristicValueChanged;
        public event CharacteristicWrittenEventHandler CharacteristicWritten;
        public event DescriptorWrittenEventHandler DescriptorWritten;
        public event CharacteristicReadEventHandler CharacteristicRead;


        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            ServicesDiscovered?.Invoke(gatt, status);
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            ConnectionStateChanged?.Invoke(gatt, status, newState);
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            CharacteristicRead?.Invoke(gatt, characteristic, status);
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            CharacteristicValueChanged?.Invoke(gatt, characteristic);
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            CharacteristicWritten?.Invoke(gatt, characteristic, status);
        }

        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            DescriptorWritten?.Invoke(gatt, descriptor, status);
        }
    }
}