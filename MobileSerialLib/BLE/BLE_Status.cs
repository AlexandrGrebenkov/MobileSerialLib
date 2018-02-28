namespace MobileSerialLib.BLE
{
    public enum BLE_Status
    {
        NotInit,//Не инициализирован ещё
        BT_NotAwailable,//блютуз не поддерживается вообще
        BLE_NotAwailable,//не поддерживается BLE
        BT_IsSwitchOff,//Блютуз выключен
        NotConnect,
        Connect,
        Error
    }
}
