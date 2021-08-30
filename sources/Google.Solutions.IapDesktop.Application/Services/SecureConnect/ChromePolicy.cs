using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    public interface IChromePolicy
    {
        Func<X509Certificate2, bool> GetAutoSelectCertificateForUrlsPolicy(
            Uri url);
    }

    public class ChromePolicy : IChromePolicy
    {
        public Func<X509Certificate2, bool> GetAutoSelectCertificateForUrlsPolicy(Uri url)
        {
            var machinePolicyMatcher = ChromeAutoSelectCertificateForUrlsPolicy
                .FromKey(RegistryHive.LocalMachine)
                .CreateMatcher(url);

            var userPolicyMatcher = ChromeAutoSelectCertificateForUrlsPolicy
                .FromKey(RegistryHive.LocalMachine)
                .CreateMatcher(url);

            return cert => machinePolicyMatcher(cert) || userPolicyMatcher(cert);
        }
    }
}
