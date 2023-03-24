using Google.Solutions.Common.Security;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Shell.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Connection
{
    public interface IRdpCredentialCallbackService
    {
        Task<RdpCredentials> GetCredentialsAsync(
            Uri callbackUrl,
            CancellationToken cancellationToken);
    }

    [Service(typeof(IRdpCredentialCallbackService))]
    public class RdpCredentialCallbackService : IRdpCredentialCallbackService
    {
        private readonly IExternalRestAdapter restAdapter;

        public RdpCredentialCallbackService(IExternalRestAdapter restAdapter)
        {
            this.restAdapter = restAdapter;
        }

        public async Task<RdpCredentials> GetCredentialsAsync( // TODO: Test
            Uri callbackUrl, 
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.restAdapter
                    .GetAsync<CredentialCallbackResponse>(callbackUrl, cancellationToken)
                    .ConfigureAwait(false);

                if (response != null)
                {
                    return new RdpCredentials(
                        response.User,
                        response.Domain,
                        SecureStringExtensions.FromClearText(response.Password));
                }
                else
                {
                    return RdpCredentials.Empty;
                }
            }
            catch (HttpRequestException e)
            {
                throw new CredentialCallbackException(
                    $"Invoking the credential callback endpoint at {callbackUrl} failed " +
                    $"and no credentials were obtained",
                    e);
            }
            catch (JsonException e)
            {
                throw new CredentialCallbackException(
                    $"The credential callback endpoint at {callbackUrl} returned " +
                    $"an invalid result",
                    e);
            }
        }

        //---------------------------------------------------------------------
        // Response entity. 
        //---------------------------------------------------------------------

        public class CredentialCallbackResponse
        {
            [JsonProperty("User")]
            public string User { get; set; }

            [JsonProperty("Domain")]
            public string Domain { get; set; }

            [JsonProperty("Password")]
            public string Password { get; set; }
        }
    }

    public class CredentialCallbackException : Exception
    {
        public CredentialCallbackException(
            string message, 
            Exception innerException) : base(message, innerException)
        {
        }
    }
}
