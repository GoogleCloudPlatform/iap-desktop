using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    /// <summary>
    /// SSH public key that has been authorized to access a resource.
    /// </summary>
    public interface IAuthorizedPublicKey
    {
        /// <summary>
        /// Email address of user.
        /// </summary>
        string Email { get; }

        /// <summary>
        /// Type of key (rsa-ssa, ...).
        /// </summary>
        string KeyType { get; }

        /// <summary>
        /// Public key.
        /// </summary>
        string PublicKey { get; }

        /// <summary>
        /// Expiry date.
        /// </summary>
        DateTime? ExpireOn { get; }
    }
}
