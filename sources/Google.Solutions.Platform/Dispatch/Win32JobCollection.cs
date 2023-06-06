using Google.Solutions.Common.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// Collection of jobs.
    /// </summary>
    public interface IWin32JobCollection : IDisposable
    {
        void Add(IWin32Job job);

        IEnumerable<IWin32Job> Jobs { get; }
    }

    public class Win32JobCollection : DisposableBase, IWin32JobCollection
    {
        private readonly ConcurrentBag<IWin32Job> jobs = new ConcurrentBag<IWin32Job>();

        //---------------------------------------------------------------------
        // IWin32JobCollection.
        //---------------------------------------------------------------------

        public IEnumerable<IWin32Job> Jobs => this.jobs;

        public void Add(IWin32Job job)
        {
            this.jobs.Add(job);
        }


        //---------------------------------------------------------------------
        // DisposableBase.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            foreach (var job in this.jobs)
            {
                job.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
