# Privacy & security

## Google APIs

IAP Desktop accesses Google Cloud Platform APIs to perform functions such as the following:

* establish [Cloud IAP TCP tunnels  :octicons-link-external-16:](https://cloud.google.com/iap/docs/tcp-forwarding-overview) to VM instances
* list VMs and obtain metadata and logs for VM instances  
* generate Windows logon credentials if requested
* publish SSH public keys if requested

The application uses the following Google APIs for this purpose:

* [OAuth 2  :octicons-link-external-16:](https://developers.google.com/identity/protocols/OAuth2)
* [Compute Engine API  :octicons-link-external-16:](https://cloud.google.com/compute/docs/reference/rest/v1/)
* [Resource Manager API  :octicons-link-external-16:](https://cloud.google.com/resource-manager/reference/rest)
* [OS Login API  :octicons-link-external-16:](https://cloud.google.com/compute/docs/oslogin/rest)
* [Logging API  :octicons-link-external-16:](https://cloud.google.com/logging/docs/reference/v2/rest)
* [Security Token Service API :octicons-link-external-16:](https://cloud.google.com/iam/docs/reference/sts/rest)

Periodically, IAP Desktop accesses the [GitHub API :octicons-link-external-16:](https://docs.github.com/en/rest) to check
for updates. After an upgrade, IAP Desktop also queries the GitHub API to download release notes.

IAP Desktop does not intentionally disclose or transmit any data to APIs other than the
ones listed on this page. 

## Usage data

By default, IAP Desktop _does not_ collect or share usage data. You can help Google improve and prioritize 
features and improvements by enabling data sharing under **Tools > Options > Data sharing**.
You can enable or disable data sharing at any time.

When you enable data sharing, IAP Desktop periodically sends anonymous usage data to the 
[Google Analytics Measurement API :octicons-link-external-16:](https://developers.google.com/analytics/devguides/collection/protocol/ga4).
Usage data includes information about which menu commands you use, any errors that occured,
and similar information.

Usage data doesn't include personal information and isn't associated with your Google account.


## Credential storage

All credentials managed by IAP Desktop are stored locally and are encrypted before storage.

### OAuth tokens

When you use the application for the first time, you have to authorize it to 
access your Google Cloud Platform on your behalf. As a result of this authorization,
the application receives an OAuth refresh token which allows it to re-authenticate 
automatically the next time you use it. This refresh token
is encrypted by using the [Windows Data Protection API :octicons-link-external-16: ](https://en.wikipedia.org/wiki/Data_Protection_API)
(DPAPI) and stored in the _current user_ part of the Windows registry. 


### Windows logon credentials

IAP Desktop allows you to save Windows logon credentials. These credentials are 
stored in the _current user_ part of the Windows registry. Like the OAuth refresh token,
all passwords are encrypted by using the DPAPI before storage.

### SSH keys

For details on how IAP Desktop stores SSH key material, see [SSH algorithms and keys](ssh-algorithms.md).

### Proxy credentials

If you've configured IAP Desktop to use an HTTP proxy server that requires authentication,
then the proxy credentials are stored in the _current user_ part of the Windows registry. 
Like the OAuth refresh token, the password is encrypted by using the DPAPI before storage.