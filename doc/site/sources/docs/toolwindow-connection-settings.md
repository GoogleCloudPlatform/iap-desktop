# Connection settings window
![Connection settings for a Windows VM](images/Connection-Settings.png){ align=right }

The **Connection settings** window lets you customize how IAP Desktop connects
to your VMs. You can configure connection settings for individual VMs or entire zones or projects.

Connection settings support inheritance: If you configure a connection setting for a project,
this setting applies to all zones and VMs in the project. Similarly, if you configure a 
connection setting for a zone, it applies to all VMs in that zone:

![Settings inheritance](images/Connection-Settings-Inheritance.png)

You can override inherited settings at lower levels. Whenever a setting deviates
from the (inherited) default, its value is shown in bold typeface.

Connection settings are stored on your local computer and don't
affect the configuration of the remote VM.

## Windows Credentials

These settings control which user account you log on with. If you don't configure Windows credentials,
IAP Desktop might prompt you for credentials when you first try to connect.

You can use the following types of user accounts:

* a local Windows user account
* an Active Directory user account

The user account must have permission to use Remote Desktop. Typically, this requires that the user is either
a member of the _Administrators_ group or the _Remote Desktop users_ group.

<table>
    <tr>
        <th>Setting</th>
        <th>Description</th>
        <th>Required</th>
    </tr>
    <tr>
        <td>Username</td>
        <td>Username or UPN, for example <code>bob</code> or <code>bob@example.com</code>.</td>
        <td>No</td>
    </tr>
    <tr>
        <td>Password</td>
        <td>Account password</td>
        <td>No</td>
    </tr>
    <tr>
        <td>Domain</td>
        <td> 
            <ul>
                <li>Leave blank if you're using an UPN as username</li>
                <li>Use the NetBIOS domain name if the username is a NetBIOS username</li>
                <li>Use <code>.</code> if you're using a local Windows user account</li>
            </ul>
        </td>
        <td>No</td>
    </tr>
</table>

## Remote Desktop Connection

These settings control how IAP Desktop connects to your VM. You can let IAP Desktop connect in one
of two ways:

*   `IapTunnel`: By default, IAP Desktop connects to the [internal IP address :octicons-link-external-16:](https://cloud.google.com/compute/docs/ip-addresses#networkaddresses)
    of your VM [through an IAP-TCP forwarding tunnel :octicons-link-external-16:](https://cloud.google.com/iap/docs/using-tcp-forwarding).
*   `Vpc`: Alternatively, you can let IAP Desktop connect to the VM's internal IP address through Cloud VPN or Interconnect. If you use this option, 
    IAP Desktop doesn't use IAP-TCP forwarding.
    
<table>
    <tr>
        <th>Setting</th>
        <th>Description</th>
        <th>Required</th>
    </tr>
    <tr>
        <td>Connect via</td>
        <td>Controls how IAP Desktop connects to your VM, see description above.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Connection timeout</td>
        <td>Timeout for connecting to the VM, in seconds.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Server port</td>
        <td>Port to connect to, typically <code>3389</code>.</td>
        <td>Yes</td>
    </tr>
</table>


## Remote Desktop Display

These settings control the display settings for Remote Desktop.

<table>
    <tr>
        <th>Setting</th>
        <th>Description</th>
        <th>Required</th>
    </tr>
    <tr>
        <td>Color depth</td>
        <td>Color depth to use.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Connection bar</td>
        <td>Controls the behavior of the connection bar that's shown when you set the Remote Desktop session to full-screen.</td>
        <td>Yes</td>
    </tr>
</table>

## Remote Desktop Resources

These settings control which local and remote resources you want to share.

<table>
    <tr>
        <th>Setting</th>
        <th>Description</th>
        <th>Required</th>
    </tr>
    <tr>
        <td>Audio mode</td>
        <td>Controls where to play back audio.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Windows shortcuts</td>
        <td>Controls whether IAP Desktop should send Windows shortcuts (such as <code>Win+R</code>) to the VM:
            <ul>
                <li>Disabled: Don't capture shortcuts and handle them locally instead.</li>
                <li>Enabled: Always capture shortcuts and handle them remotely.</li>
                <li>FullScreenOnly: Only capture shortcuts when the Remote Desktop session is in full-screen mode.</li>
            </ul>
        </td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect clipboard</td>
        <td>Share the local clipboard with the remote VM.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect printers</td>
        <td>Share local printers with the remote VM.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect smart cards</td>
        <td>Share local smart cards with the remote VM.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect local ports</td>
        <td>Share local ports with the remote VM.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect drives</td>
        <td>Share local drives with the remote VM.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect devices</td>
        <td>Share local devices with the remote VM.</td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Redirect WebAuthn authenticators</td>
        <td>Share WebAuthn authenticators with the remote VM.</td>
        <td>Yes</td>
    </tr>
</table>



## Remote Desktop Security Settings

These settings control which RDP security mechanism to apply.

<table>
    <tr>
        <th>Setting</th>
        <th>Description</th>
        <th>Required</th>
    </tr>
    <tr>
        <td>Network level authentication</td>
        <td>
            Controls whether to secure connection using <a href='https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/clients/remote-desktop-allow-access'>network level authentication</a>
            (NLA). 
            <br><br>
            Leave NLA enabled unless you're connecting to a VM that uses a custom credential service provider such as the <a href='https://tools.google.com/dlpage/gcpw/'>Google Credential Provider for Windows</a>.
            <br><br>
            Disabling NLA automatically enables server authentication.
        </td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Server authentication</td>
        <td>
            Controls whether to verify the VM's TLS certificate.
            <br><br>
            By default, IAP Desktop connects to the VM through an IAP-TCP forwarding tunnel. Tunneled connections
            use a hostname such as <code>localhost:12345</code>, which is different from the VM's canonical hostname.
            Because of this mismatch, certificate verification is unlikely to succeed and therefore off by default.
        </td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Restricted Admin mode</td>
        <td>
            Controls whether to use <a href='https://learn.microsoft.com/en-us/windows/security/identity-protection/remote-credential-guard?tabs=intune'>Restricted Admin mode</a>,
            which disables the transmission of reusable credentials to the VM. 
            <br><br>
            To use Restricted Admin mode, you must ensure that the following prerequisites are met:
            <ul>
                <li>You're using a user account that has local administrator privileges on the VM.</li>
                <li>You've configured the VM to <a href='https://social.technet.microsoft.com/wiki/contents/articles/32905.remote-desktop-services-enable-restricted-admin-mode.aspx'>permit Restricted Admin mode</a>.</li>
            </ul>
            If you don't meet these prerequisites, connecting to the VM might fail with the error <i>Account restrictions are preventing this user from signing in</i>.
        </td>
        <td>Yes</td>
    </tr>
</table>

## SSH Connection

These settings control how IAP Desktop connects to your VM. The settings are analogous to the 
[Remote Desktop Connection](#remote-desktop-connection) settings.

## SSH Credentials

These settings control which user account you log on with, and which authentication method to use.

IAP Desktop supports the following SSH authentication methods:

*   `publickey`
*   `password`
*   `keyboard-interactive`

When you use `publickey`, IAP Desktop automatically publishes a public key to the VM, and uses the corresponding private key to authenticate.
Depending on the VM's configuration, IAP Desktop uses either [OS Login :octicons-link-external-16:](https://cloud.google.com/compute/docs/oslogin)
or [metadata keys :octicons-link-external-16:](https://cloud.google.com/compute/docs/connect/add-ssh-keys) to publish the public key.


<table>
    <tr>
        <th>Setting</th>
        <th>Description</th>
        <th>Required</th>
    </tr>
    <tr>
        <td>Public key authentication</td>
        <td>
            Controls the authentication method to use:
            
            <ul>
                <li>Enabled: Use <code>publickey</code> authentication and let IAP Desktop automatically create an SSH key and publish it using OS Login or metadata keys. </li>
                <li>Disabled: Use <code>password</code> or <code>keyboard-interactive</code>-based authentication.</li>
            </ul>
        </td>
        <td>Yes</td>
    </tr>
    <tr>
        <td>Username</td>
        <td>
            Linux/Unix username. 
            <br><br>
            This setting is ignored when using OS Login because OS Login automatically determines your username.
        </td>
        <td>No</td>
    </tr>
    <tr>
        <td>Password</td>
        <td>Linux/Unix password</td>
        <td>No</td>
    </tr>
</table>
