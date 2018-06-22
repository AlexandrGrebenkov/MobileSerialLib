using System;

namespace MobileSerialLib.BLE
{
    public interface IMSerial_BLE : IMSerial
    {
        /// <summary>
        /// Состояние сетевого интерфейса
        /// </summary>
        BLE_Status Status { get; set; }
        /// <summary>
        /// Инициализация модуля Bluetooth. производится проверки на наличие Bluetooth-модуля, его состоянии и доступности
        /// Обязательный вызов перед использованием!
        /// </summary>
        /// <returns>Возвращает статус</returns>
        BLE_Status BLE_Init();
        /// <summary>
        /// Запуск поиска BLE-устройств
        /// </summary>
        /// <param name="execute">Сюда попадаем каждый раз при обнаружении нового девайса в эфире</param>
        void StartScan(Action<BLE_Device_Info> execute);
        /// <summary>
        /// Отменяем поиск приборов
        /// </summary>
        void StopScan();
        /// <summary>
        /// Выбор девайса по его имени 
        /// </summary>
        /// <param name="name"></param>
        void SelectDeviceByName(string name);
        void SelectDeviceByAddress(string address);

        void Close();
    }
}
