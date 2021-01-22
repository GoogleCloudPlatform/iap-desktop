using Google.Solutions.Common.Auth;
using Google.Solutions.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    public interface IOsLoginAdapter
    {
        Task<LoginProfile> ImportSshPublicKeyAsync(
            string projectId,
            ISshKey key,
            TimeSpan validity,
            CancellationToken token);
    }

    public class LoginProfile
    {
        public string PosixUsername { get; }

        public LoginProfile(string posixUsername)
        {
            this.PosixUsername = posixUsername;
        }

        public static LoginProfile Create(
            string preferredUsername)
        {
            throw new NotImplementedException();
        }
    }
}
