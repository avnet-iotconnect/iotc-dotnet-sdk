using System.ComponentModel;

namespace iotdotnetsdk.common.Enums
{
    public enum AuthType
    {
        [Description("1")]
        Token = 1,
        [Description("2")]
        SelfSigned = 2,
        [Description("3")]
        CASigned = 3,
        [Description("4")]
        TPM = 4,
        [Description("5")]
        SymmetricKey = 5,
        [Description("BootstrapCertificate")]
        BootstrapCertificate = 6,
        [Description("7")]
        CASignedIndividual = 7,
        //[Description("x509")]
        //BootstrapCertificate = 7,
        //[Description("sigplus")]
        //PKISignature = 8,
    }
}
