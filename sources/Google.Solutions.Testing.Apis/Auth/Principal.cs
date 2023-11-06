using Google.Apis.CloudResourceManager.v1;
using Google.Solutions.Testing.Apis.Integration;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis.Auth
{
    internal abstract class Principal
    {
        private readonly CloudResourceManagerService crmService;

        protected Principal(
            CloudResourceManagerService crmService,
            string username)
        {
            this.crmService = crmService;
            this.Username = username;
        }

        public string Username { get; }

        protected abstract string PolicyPrefix { get; }

        public async Task GrantRolesAsync(string[] roles)
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var policy = await this.crmService.Projects
                    .GetIamPolicy(
                        new Google.Apis.CloudResourceManager.v1.Data.GetIamPolicyRequest(),
                        TestProject.ProjectId)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                foreach (var role in roles)
                {
                    policy.Bindings.Add(
                        new Google.Apis.CloudResourceManager.v1.Data.Binding()
                        {
                            Role = role,
                            Members = new string[] { $"{this.PolicyPrefix}:{this.Username}" }
                        });
                }

                try
                {
                    await this.crmService.Projects
                        .SetIamPolicy(
                            new Google.Apis.CloudResourceManager.v1.Data.SetIamPolicyRequest()
                            {
                                Policy = policy
                            },
                            TestProject.ProjectId)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    break;
                }
                catch (GoogleApiException e) when (e.Error != null && e.Error.Code == 409)
                {
                    //
                    // Concurrent modification - back off and retry. 
                    //
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
        }
    }
}
