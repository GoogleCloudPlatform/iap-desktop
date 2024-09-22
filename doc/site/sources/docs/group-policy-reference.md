# Group policy reference

You can use Active Directory group policies to configure policies for IAP Desktop. Policies take
~~~~precedence over user settings: When you configure a policy, users can't change the respective
setting anymore. 

IAP Desktop supports the following policies:

| Policy                                        | Default | Synopsis                                                                                  |
| --------------------------------------------- | ------- |----------------------------------------------------------------------------------------- |
| Enable update checks                          | On      | Periodically check for updates on exit. |
| Enable data sharing                           | Off     | Share anonymous usage data to help Google improve and prioritize features. |
| Enable BeyondCorp certificate-based access    | Off     | Secure connections to Google Cloud by using BeyondCorp certificate-based access.    |
| Enable Workforce Identity                     | -       | Set provider to use for workforce identity federation. |
| Enable Private Service Connect                | Off     | Use custom Private Service Connect endpoint to connect to connect to Google APIs.  |
| Enable HTTPS proxy                            | -       | Set proxy server or autoconfiguration URL. |
| SSH key type                                  | -       | Set key type to use for SSH public key authentication. |
| SSH metadata key validity                     | -       | Lifetime for SSH keys published to Compute Engine instance metadata and OS Login. |

For more information about using group policies to manage IAP Desktop, see
[Use group policies to manage IAP Desktop](group-policies.md).