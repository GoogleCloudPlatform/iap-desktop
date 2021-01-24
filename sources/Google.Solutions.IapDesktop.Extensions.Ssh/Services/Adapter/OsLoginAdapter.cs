using Google.Solutions.Common.Auth;
using Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth;
using Google.Solutions.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Adapter
{
    public interface IOsLoginAdapter
    {
        Task<AuthorizedKey> ImportSshPublicKeyAsync(
            string projectId,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token);
    }
}
