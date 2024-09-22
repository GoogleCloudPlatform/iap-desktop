???+ info "Required roles"

    To follow the steps in this guide, you need the following roles:
    
    *   [ ] [IAP-Secured Tunnel User :octicons-link-external-16:](https://cloud.google.com/iap/docs/managing-access#roles) on
        the project or VM.
        
        You can launch IAP Desktop from within a web browser by using `iap-rdp:///` links. 

## Enable the browser integration feature

The browser integration feature is disabled by default. To enable it, do the following:

*   Open IAP Desktop.
*   Go to **Tools > Options**
*   On the **General** tab, set **Allow launching IAP Desktop from a web browser** to **enabled**.


## Connect by URL

You can now open IAP Desktop and connect to a VM by pointing your browser to a `iap-rdp:///` URL
such as:

```
iap-rdp:///project-id/zone-id/instance-name
```

Where:

* `project-id` is the ID of the project, for example `my-project-123`.
* `zone-id` is the ID of the zone a VM is running in, for example `us-central1-a`.
* `instance-name` is the name of the VM instance, for example `my-instance-1`.

???+ Note

    Instead of a triple-slash, you can also use a single slash: `iap-rdp:/project-id/zone-id/instance-name`.

### Parameters

Optionally, you can add additional parameters to the URL to customize the connection behavior:

```
iap-rdp:///my-project-123/us-central1-a/my-instance-1?Username=bob&DesktopSize=1
```

The following parameters are supported:

<table>
<tr>
    <th>Parameter</th>
    <th>Value</th>
</tr>
<tr>
    <td><code>Username</code></td>
    <td>Windows username (SAM format)</td>
</tr>
<tr>
    <td><code>Domain</code></td>
    <td>Domain (NetBIOS format)</td>
</tr>
<tr>
    <td><code>RdpPort</code></td>
    <td>RDP port the server is listening on. Use this parameter if you've configured Windows <a href='https://docs.microsoft.com/en-us/windows-server/remote/remote-desktop-services/clients/change-listening-port'>to use a listening port other than 3389 </a>.
    </td>
</tr>
<tr>
    <td><code>ConnectionBar</code></td>
    <td>Controls whether the connection bar is shown in full-screen mode:
        <ul>
            <li><code>0</code> - automatically hide (default)</li>
            <li><code>1</code> - pinned</li>
            <li><code>2</code> - off</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>DesktopSize</code></td>
    <td>Controls the remote desktop resolution/size:
        <ul>
            <li><code>0</code> - same as client size</li>
            <li><code>1</code> - same as screen size</li>
            <li><code>2</code> - automatically adjust (default)</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>ColorDepth</code></td>
    <td>Controls the color depth of the remote desktop:
        <ul>
            <li><code>0</code> - high color</li>
            <li><code>1</code> - true color (default)</li>
            <li><code>2</code> - deep color</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>AudioMode</code></td>
    <td>Controls how audio is played:
        <ul>
            <li><code>0</code> - play locally (default)</li>
            <li><code>1</code> - play on server</li>
            <li><code>2</code> - do not play</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RedirectClipboard</code></td>
    <td>Controls whether clipboard contents are shared with remote desktop:
        <ul>
            <li><code>0</code> - disabled</li>
            <li><code>1</code> - enabled (default)</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RdpRedirectPrinter</code></td>
    <td>Controls whether local printers are shared with remote desktop:
        <ul>
            <li><code>0</code> - disabled (default)</li>
            <li><code>1</code> - enabled</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RdpRedirectSmartCard</code></td>
    <td>Controls whether local smart cards are shared with remote desktop:
        <ul>
            <li><code>0</code> - disabled (default)</li>
            <li><code>1</code> - enabled</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RdpRedirectPort</code></td>
    <td>Controls whether local ports are shared with remote desktop:
        <ul>
            <li><code>0</code> - disabled (default)</li>
            <li><code>1</code> - enabled</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RdpRedirectDrive</code></td>
    <td>Controls whether local drives are shared with remote desktop:
        <ul>
            <li><code>0</code> - disabled (default)</li>
            <li><code>1</code> - enabled</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RdpRedirectDevice</code></td>
    <td>Controls whether local devices are shared with remote desktop:
        <ul>
            <li><code>0</code> - disabled (default)</li>
            <li><code>1</code> - enabled</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>RdpHookWindowsKeys</code></td>
    <td>Controls whether the remote desktop handles Windows shortcuts (like Win+X):
        <ul>
            <li><code>0</code> - never</li>
            <li><code>1</code> - always</li>
            <li><code>2</code> - only when set to full-screen (default)</li>
        </ul>
    </td>
</tr>
<tr>
    <td><code>CredentialGenerationBehavior</code></td>
    <td>Controls whether the user is offered to generate new credentials when connecting:
        <ul>
            <li><code>0</code> - allow generating new credentials</li>
            <li><code>1</code> - allow generating new credentials if no existing credentials found (default)</li>
            <li><code>2</code> - do not allow generating new credentials</li>
            <li><code>3</code> - force user to generate new credentials</li>
        </ul>

   The parameter is ignored when you use <code>CredentialCallbackUrl</code>.
    </td>
</tr>
<tr>
    <td><code>CredentialCallbackUrl</code></td>
    <td>Callback URL for Windows logon credentials. When provided, IAP Desktop sends an HTTP 
        <code>GET</code> request to this URL and expects a response in the following format:
        <pre>
{
    Domain: "domain",
    User: "user",
    Password: "password"
}</pre>
        IAP Desktop then uses these credentials to automatically log on the user.
       The response must use the content type <code>application/json</code>.

Use URL signing or similar mechanisms to ensure that callback URLs can only be retrieved
once, or stay valid for a limited period of time only.
    </td>
</tr>
</table>

### Limitations

* URLs can't contain Windows passwords. To automatically log on users, specify a `CredentialCallbackUrl`.
* Connecting to VM instances from within a web browser is currently not supported for SSH.

## What's next

*   Read more about [connect to Windows VMs by using Remote Desktop](connect-windows.md)
*   Learn how you can [connect to Linux VMs by using SSH](connect-linux.md)
