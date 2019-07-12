using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Google.CloudIap.Util
{
    public class LazyWithRetry<T> where T : class
    {
        private readonly Func<T> valueFactory;
        private readonly object instanceLock = new object();
        private T instance;

        public LazyWithRetry(Func<T> valueFactory)
        {
            this.valueFactory = valueFactory;
            this.instance = null;
        }

        public T Value
        {
            get
            {
                lock (instanceLock)
                {
                    if (this.instance == null)
                    {
                        // Create new instance. If it throws an exception, we will
                        // simply retry next time. System.Lazy, in contrast, would
                        // cache and rethrow an exception in this case.
                        this.instance = this.valueFactory();
                    }

                    return this.instance;
                }
            }
        }
    }
}
