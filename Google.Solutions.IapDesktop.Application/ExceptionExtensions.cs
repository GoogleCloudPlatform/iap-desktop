using System;

namespace Google.Solutions.IapDesktop.Application
{
    public static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception e)
        {
            if (e is AggregateException aggregate)
            {
                e = aggregate.InnerException;
            }

            return e;
        }
    }
}
