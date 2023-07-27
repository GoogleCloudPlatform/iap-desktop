# Proxy Configuration

You can configure IAP Desktop to use a proxy server to access Google Cloud APIs.

IAP Desktop supports 3 ways to configure proxy server settings:

1.  **System** (_Use system settings_): IAP Desktop
    obtains proxy server settings from Windows. You can change these settings by using
    the Windows control panel.
2.  **Manual**: Explicitly provide a server name and port to use as HTTPS
    proxy server.
3.  **Auto-config**: Specify a URL to a proxy autoconfiguration (PAC) file.
    IAP Desktop downloads and evaluates the file and applies proxy settings accordingly.

When you use (2) or (3), you can optionally specify a username and password if your
proxy server requires authentication. 

All proxy server settings can be viewed
and modified under **Tools > Options > Network**:

![Proxy settings](images/Proxy-Settings.png)


## Filtering and SSL inspection

If your organization uses a proxy server that performs filtering and SSL inspection, 
some additional configuration might be required to allow users to use IAP Desktop.

### Proxy CA certificates

IAP Desktop uses the Windows _Trusted Root Certification Authorities Certificate Store_
for verifying TLS certificates. If your proxy server performs SSL inspection and therefore
re-encrypts traffic, make sure to add the proxy server's CA certiticate to this
certificate store.

### Allow-list for Domains accessed by IAP Desktop

To let IAP Desktop communicate with Google Cloud APIs, make sure that your proxy server 
permits HTTPS communication to the following domains:

* `https://oauth2.googleapis.com`
* `https://openidconnect.googleapis.com`
* `https://compute.googleapis.com`
* `https://oslogin.googleapis.com`
* `https://cloudresourcemanager.googleapis.com`
* `https://logging.googleapis.com`

The IAP TCP forwarding tunnels that IAP Desktop uses to create SSH and RDP connections
use WebSockets. Make sure that your proxy server permits WebSocket communication to the following domain:

* `https://tunnel.cloudproxy.app`

???+ Note
    Squid (and possibly other proxy servers) does not allow WebSocket
    connections when configured to perform SSL inspection (_bumping_). To allow
    WebSocket communication, exclude `tunnel.cloudproxy.app` from SSL termination
    by letting Squid [splice :octicons-link-external-16:](https://wiki.squid-cache.org/Features/SslPeekAndSplice)
    connections to this domain.


## Endpoint Verification

When you enable Endpoint Verification, IAP Desktop uses mutual TLS (mTLS) for all
Google APIs and for IAP TCP forwarding. mTLS is not compatible with SSL inspection.

To use Endpoint Verification, exclude the following domains from SSL inspection.

* `https://oauth2.mtls.googleapis.com`
* `https://compute.mtls.googleapis.com`
* `https://oslogin.mtls.googleapis.com`
* `https://cloudresourcemanager.mtls.googleapis.com`
* `https://logging.mtls.googleapis.com`
* `https://mtls.tunnel.cloudproxy.app`

???+ Note
    If you use Squid, you can exclude domains from inspection by 
    configuring them to use [splicing :octicons-link-external-16:](https://wiki.squid-cache.org/Features/SslPeekAndSplice)
    instead of _bumping_.
