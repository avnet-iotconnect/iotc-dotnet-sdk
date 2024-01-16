using System;
using System.Net;

namespace iotdotnetsdk.common.Models
{
    public class DeviceNotFoundException : Exception
    {
        public DeviceNotFoundException(string message) : base(message)
        {

        }
    }

    public class SDKSerializationException : Exception
    {
        private string value;
        public SDKSerializationException(string message, string value) : base(message)
        {
            this.value = value;
        }
    }

    public class DiscoveryException : Exception
    {
        public DiscoveryException(string message) : base(message)
        {

        }
    }

    public class SyncException : Exception
    {
        public SyncException(HttpStatusCode statusCode)
        {

        }
        public SyncException(string message) : base(message)
        {

        }
    }

    public class UnhandledStatusCodeException : Exception
    {
        private HttpStatusCode statusCode;
        public UnhandledStatusCodeException(HttpStatusCode statusCode)
        {
            this.statusCode = statusCode;
        }
    }

    public class UnhandledException : Exception
    {
        private Exception exception;
        public UnhandledException(string message, Exception exception) : base(message)
        {
            this.exception = exception;
        }
    }

    public class NoInternetException : Exception
    {
        public NoInternetException() : base("Internet Connectivity Not Found!")
        {

        }
    }

    public class DeviceNotAcquiredException : Exception
    {
        public DeviceNotAcquiredException(string message) : base(message)
        {

        }
    }

    internal class InternalException : Exception
    {
        string message;
        Exception exception;
        internal InternalException(string message, Exception exception)
        {
            this.message = message;
            this.exception = exception;
        }
    }

    internal class DeviceUnauthorizedException : Exception
    {
        string message;
        Exception exception;
        internal DeviceUnauthorizedException(string message, Exception exception)
        {
            this.message = message;
            this.exception = exception;
        }

        internal string HubMessage { get { return message; } }
    }

    public class SDKInitializationException : Exception
    {
        public SDKInitializationException(string message) : base(message)
        {

        }
        private Exception exception;
        public SDKInitializationException(string message, Exception exception) : base(message)
        {
            this.exception = exception;
        }
    }

    public class CompanyNotFoundException : Exception
    {
        public CompanyNotFoundException(string message) : base(message)
        {

        }
    }

    public class SubscriptionExpiredException : Exception
    {
        public SubscriptionExpiredException(string message) : base(message)
        {

        }
    }
}
