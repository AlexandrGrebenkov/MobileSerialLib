using System;
using Android.Bluetooth;

namespace AndroidBluetoothLE.Bluetooth.Client
{
    public delegate void ReceivedWriteResponceEventHandler(
        BluetoothGattCharacteristic characteristic, GattStatus status);
    public delegate void ReceivedReadResponceEventHandler(byte[] data);

    public class DeviceWritingHandler : IDisposable
    {
        private readonly BluetoothGatt _gatt;
        private readonly GattClientObserver _gattObserver;

        public event ReceivedWriteResponceEventHandler ReceivedWriteResponce;
        public event ReceivedReadResponceEventHandler ReceivedReadResponce;

        public DeviceWritingHandler(BluetoothGatt gatt, GattClientObserver gattObserver)
        {
            _gatt = gatt;
            _gattObserver = gattObserver;
            _gattObserver.CharacteristicWritten += OnCharacteristicWritten;//Change!!! Bug?
            _gattObserver.CharacteristicValueChanged += _gattObserver_CharacteristicValueChanged;
        }

        private void _gattObserver_CharacteristicValueChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            ReceivedReadResponce?.Invoke(characteristic.GetValue());
        }

        public void Dispose()
        {
            _gattObserver.CharacteristicWritten -= OnCharacteristicWritten;
        }

        public void Write(byte[] buffer, BluetoothGattCharacteristic characteristic, bool withResponce = false)
        {
            WriteValueInternal(buffer, characteristic, withResponce);
        }

        private void OnCharacteristicWritten(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            ReceivedWriteResponce?.Invoke(characteristic, status);
        }

        private void WriteValueInternal(byte[] buffer, BluetoothGattCharacteristic characteristic, bool withResponce)
        {
            characteristic.SetValue(buffer);
            characteristic.WriteType = withResponce ? GattWriteType.Default : GattWriteType.NoResponse;
            _gatt.WriteCharacteristic(characteristic);
            
        }

        public void ClearAllReadEvents()
        {
            ReceivedReadResponce = data => { };
            /*return;
            foreach (ReceivedReadResponceEventHandler d in ReceivedReadResponce.GetInvocationList())
            {
                ReceivedReadResponce -= d;
            }*/
        }
    }
}