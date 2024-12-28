using System;

namespace MERX.Features.Misc.Opus
{
    public class OpusException : Exception
    {
        public OpusException(OpusStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public readonly OpusStatusCode StatusCode;
    }
}