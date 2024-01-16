using System.ComponentModel;

namespace iotdotnetsdk.common.Enums
{
    public enum ErrorCodes
    {
        [Description("No Error")]
        Ok = 0,

        [Description("Device not found. Device is not whitelisted to platform")]
        DeviceNotFound = 1,

        [Description("Device is not active")]
        DeviceNotActive = 2,

        [Description("Un-Associated. Device has not any template associated with it")]
        DeviceUnAssociated = 3,

        [Description("Device is not acquired. Device is created but it is in release state")]
        DeviceNotAcquired = 4,

        [Description("Device is disabled. Its disabled from IoTHub by Platform Admin")]
        DeviceDisabled = 5,

        [Description("Company not found as SID is not valid")]
        CompanyNotFound = 6,

        [Description("Subscription is expired")]
        SubscriptionExpired = 7,

        [Description("Connection Not Allowed")]
        ConnectionNotAllowed = 8,
    }
}