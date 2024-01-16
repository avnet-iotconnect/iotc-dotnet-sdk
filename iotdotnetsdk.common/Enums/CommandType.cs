namespace iotdotnetsdk.common.Enums
{
    public enum CommandType
    {
        DEVICE_COMMAND = 0,
        OTA_COMMAND = 1,
        MODULE_COMMAND = 2,
        REFRESH_ATTRIBUTE = 101,
        REFRESH_TWIN = 102,
        REFRESH_EDGE_RULE = 103,
        REFRESH_CHILD_DEVICE = 104,
        DATA_FREQUENCY_CHANGE = 105,
        DEVICE_DELETED = 106,
        DEVICE_DISABLED = 107,
        DEVICE_RELEASED = 108,
        STOP_OPERATION = 109,
        START_HEARTBEAT = 110,
        STOP_HEARTBEAT = 111,
        DEVICE_LOG_COMMAND = 112,
        SKIP_DATA_VALIDATION_COMMAND = 113,
        SEND_SDK_LOG_COMMAND = 114,
        DEVICE_CONNECTION_STATUS = 116
    }
}