using System;
namespace FluentEmojiBrowser
{
    public class SvgImageErrorEventArgs : EventArgs
    {
        public SvgImageErrorEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
        }

        public string Message { get; }
        public Exception Exception { get; }
    }

}

