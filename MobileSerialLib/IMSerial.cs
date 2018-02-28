using System;
using System.Threading.Tasks;

namespace MobileSerialLib
{
    public interface IMSerial
    {        
        /// <summary>
        /// Подключение к устройству
        /// </summary>
        void Connect(Action<bool> action);
        /// <summary>
        /// Отключение от устройства
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Метод чтения
        /// </summary>
        /// <param name="timeout">Таймаут (в мс)</param>
        /// <returns>Возвращает принятый буфер</returns>
        byte[] Read(uint timeout = 1000);
        /// <summary>
        /// Метод чтения
        /// </summary>
        /// <param name="timeout">Таймаут (в мс)</param>
        /// <returns>Возвращает принятый буфер</returns>
        Task<byte[]> ReadAsync(uint timeout = 1000);
        /// <summary>
        /// Запись масссива в устройство
        /// </summary>
        /// <param name="TxBuff">Массив данных</param>
        /// <param name="timeout">Максимальное допустимое время выполнения</param>
        void Write(byte[] TxBuff, int timeout = 1000);

        void RxCallback(Action<byte[]> execute);
    }
}
