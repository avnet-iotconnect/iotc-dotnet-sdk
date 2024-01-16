namespace iotdotnetsdk.common.Enums
{
    public enum MessageTypes
    {
        RPT = 0,
        RPT_EDGE = 1,
        RULE_MATCHED_EDGE = 2,
        FLT = 3,
        OFFLINE_DATA = 4,
        HEART_BEAT = 5,
        ACK = 6,
        DEVICE_LOG = 7,

        INFO = 200,
        ATTRS = 201,
        SHADOW = 202,
        EDGE_RULE = 203,
        DEVICES = 204,
        PENDING_OTA = 205,
        ALL_DATA = 210,
        
        CREATE_CHILD_DEVICE = 221,
        DELETE_CHILD_DEVICE = 222
    }
}