using Google.Solutions.IapDesktop.Application.Services.Integration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Workflows
{
    internal class SynchronousJobService : IJobService
    {
        public Task<T> RunInBackground<T>(JobDescription jobDescription, Func<CancellationToken, Task<T>> jobFunc)
        {
            return jobFunc(CancellationToken.None);
        }
    }
}
