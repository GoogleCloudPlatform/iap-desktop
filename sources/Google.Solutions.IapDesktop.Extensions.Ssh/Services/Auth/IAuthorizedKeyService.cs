using Google.Solutions.Common.Locator;
using Google.Solutions.Ssh;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    public interface IAuthorizedKeyService : IDisposable
    {
        Task<AuthorizedKey> AuthorizeKeyAsync(
            InstanceLocator instance,
            ISshKey key,
            TimeSpan keyValidity,
            string preferredPosixUsername,
            AuthorizeKeyMethods methods,
            CancellationToken token);
    }

    [Flags]
    public enum AuthorizeKeyMethods
    {
        InstanceMetadata = 1,
        ProjectMetadata = 2,
        Oslogin = 4,
        All = 7
    }

}
