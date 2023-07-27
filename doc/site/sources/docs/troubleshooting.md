# Troubleshoot common issues

## Installation

### :material-message-alert: "You need administrative privileges to install IAP Desktop on Windows Server"

On Windows Server, the [DisableUserInstalls :octicons-link-external-16:](https://docs.microsoft.com/en-us/windows/win32/msi/disableuserinstalls)
policy disables per-user installations by default. To install IAP Desktop on Windows Server, use one of the following options:<br><br>

#### A. Install as elevated user

You can sidestep the DisableUserInstalls policy by launching the installer as an elevated user:

1. Start an elevated command prompt
1. Launch the installer: `msiexec /i "IapDesktop.msi"`


#### B. Change the DisableUserInstalls policy

You can permanently change the DisableUserInstalls policy by editing the local group policy on the server:

1.  In the Group Policy Editor navigate to **Computer Configuration > Administrative Templates > Windows Components > Windows Installer**
2.  Open the **Prohibit User Installs** policy and configure the following settings:
    
    +    **Status**: **Enabled**
    +    **User install behavior**: **Allow user installs**
    
    Click **OK**.
3.  Open the **Turn off Windows Installer** policy and configure the following settings:
    
    +    **Status**: **Enabled**
    +    **Disable Windows Installer**: **Never**
    
    Click **OK**.
5.  Retry the installation.

####C. Performing an administrative installation

If you don't have administrative privileges, you can perform an _administrative install_, which extracts the files of the MSI package to a local folder:

1.  Open a command prompt.
2.  Run the installer 

        msiexec /A "%USERPROFILE%\Downloads\IapDesktop.msi" TARGETDIR="%APPDATA%" /QB!
    
    +   `/A` instructs msiexec to perform an administrative install
    +   `TARGETDIR` specifies the directory to extract files to
    +   `/QB!` runs the installation silently
        
???+ Note

    The administrative install won't create an entry in the Start menu. To launch IAP Desktop, run:

        %APPDATA%\Google\IAP Desktop\IapDesktop.exe

## Remote Desktop

### :material-message-alert: "Your credentials did not work" dialogs are shown, despite saved credentials

**Symptom**: You've configured valid credentials, but each time you try to connect to a VM, the _Your credentials did not work_ dialog appears. 
After re-entering the same credentials again, the connection succeeds.

This issue can be caused by the [Always prompt for password upon connection :octicons-link-external-16:](https://admx.help/?Category=Windows_10_2016&Policy=Microsoft.Policies.TerminalServer::TS_PASSWORD) 
group policy setting. This policy is configured by default on [CIS hardened images :octicons-link-external-16:](https://www.cisecurity.org/cis-hardened-images/google/).

IAP Desktop cannot distinguish between genuine authentication failures and prompts triggered by this policy.

### Credentials are rejected


**Symptom**: You've configured valid credentials, but each time you try to connect to a VM, the _Your credentials did not work_ 
dialog appears. Re-entering the credentials does not solve the issue.

This behavior can occur if the [LAN Manager authentication level :octicons-link-external-16:](https://docs.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/network-security-lan-manager-authentication-level) of your local workstation and the VM are incompatible.

Because of the way IAP Desktop tunnels RDP connections, it always uses NTLM for authentication and can't use Kerberos. 
Depending on the LAN Manager authentication level configured on both machines, authentication will either use NTLM or NTLMv2.
If you've configured the VM to demand NTLMv2 (authentication level `5`), but your local workstation uses level `1`, `2`, or `3`, protocol 
negotiation fails and your credentials are rejected.

To solve this issue, make sure that the LAN Manager authentication level on both machines is compatible.


### Other errors

If you are seeting other error messages, try manually establishing a Cloud IAP TCP forwarding tunnel:

1.  If you have not installed the Cloud SDK yet, 
    [download and install it first :octicons-link-external-16:](https://cloud.google.com/sdk/docs/downloads-interactive).
1.  Open a command prompt window (`cmd.exe`).
1.  Run the following command: 
    
        gcloud compute start-iap-tunnel INSTANCE 3389 --project=PROJECT --zone=ZONE --local-host-port=localhost:13389
         
    Replace `INSTANCE` by the name of an instance and `PROJECT` and `ZONE` by 
    the project and zone the instance is located in.
    
1.  Wait for the output `Listening on port [13389].` to appear.
1.  Launch `mstsc.exe` and try to connect to `localhost:13389`.

If establishing the tunnel does not work, check if a local firewall is blocking `gcloud`
from binding to a local port or blocking communication with Cloud IAP.
