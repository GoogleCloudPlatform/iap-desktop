# Troubleshooting Remote Desktop issues

## :material-message-alert: Copy/paste doesn't work

**Symptom**: Unable to copy and paste text or files over RDP

This behavior can be caused by an invalid configuration or group policies.

### Verify your connection settings in IAP Desktop 

IAP Desktop lets you enable or disable clipboard sharing for individual VMs, entire zones, or projects. To verify
that clipboard sharing is enabled for the affected VM, do the following:

1.  In the **Project Explorer** window, right-click the affected VM and select **Connection settings**.
1.  In the **Connection settings** window, under **Remote Desktop Resources** verify that **Redirect clipboard** is set to **enabled**.

### Verify `rdpclip` is running

Clipboard sharing requires that `rdpclip.exe` is running in your remote session. To verify that this
process is running, do the following:

1.  On the remote VM, right-click the **Start** button and select **Windows PowerShell**.
1.  Run the following PowerShell command:

        Get-Process | 
          ? {$_.SessionId -eq (Get-Process -PID $PID).SessionId} | 
          ? {$_.ProcessName -eq "rdpclip"}

    You should see output similar to the following:
    
        Handles  NPM(K)    PM(K)      WS(K)     CPU(s)     Id  SI ProcessName
        -------  ------    -----      -----     ------     --  -- -----------
            345      14     3564       8164       4.44   3156   2 rdpclip

    If the output doesn't indicate a running `rdpclip` process, restart `rdpclip` or try signing out
    and signing in again.

### Check local policies

If your local computer is managed by an organization, it's possible that your organization
has [applied a policy that disables copy/paste for RDP :octicons-link-external-16:](https://learn.microsoft.com/en-us/azure/virtual-desktop/configure-device-redirections#disable-redirection-on-the-local-device).
To check if this is the case, do the following:

1.  On your local computer, right-click the **Start** button and select **Windows PowerShell**.
1.  Run the following PowerShell command:

        "HKLM:\SOFTWARE\Microsoft\Terminal Server Client",
        "HKCU:\SOFTWARE\Microsoft\Terminal Server Client",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Terminal Server Client",
        "HKCU:\SOFTWARE\WOW6432Node\Microsoft\Terminal Server Client" |
          % {Get-ItemProperty -Path $_ -Name "DisableClipboardRedirection" -ErrorAction SilentlyContinue } | 
          ? {$_.DisableClipboardRedirection -eq 1}

    If you see output similar to the following, then there is a policy that disallows copy and paste:
    
        DisableClipboardRedirection : 1
        PSPath                      : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Terminal Server Client
        PSParentPath                : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft
        PSChildName                 : Terminal Server Client
        PSDrive                     : HKLM
        PSProvider                  : Microsoft.PowerShell.Core\Registry

    If the output is empty, then your local computer allows copy/paste, but it's possible that the remote VM doesn't allow it.
    
### Check remote policies

If the remote VM is managed by an organization, it's possible that your organization
has [applied a policy that disables copy/paste for RDP :octicons-link-external-16:](https://admx.help/?Category=Windows_10_2016&Policy=Microsoft.Policies.TerminalServer::TS_CLIENT_CLIPBOARD).
To check if this is the case, do the following:


1.  On the remote VM, right-click the **Start** button and select **Windows PowerShell**.
1.  Run the following PowerShell command:

        "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services",
        "HKCU:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services" |
          % {Get-ItemProperty -Path $_ -Name "fDisableClip" -ErrorAction SilentlyContinue } | 
          ? {$_.fDisableClip -eq 1}


    If you see output similar to the following, then there is a policy that disallows copy and paste:
    
        fDisableClip : 1
        PSPath       : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services
        PSParentPath : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows NT
        PSChildName  : Terminal Services
        PSDrive      : HKLM
        PSProvider   : Microsoft.PowerShell.Core\Registry

    If the output is empty, then the VM allows copy/paste.
    
### Type clipboard text

In situations where copy/paste is disallowed by policy, you can copy text from your local computer to a VM
by using the **Type clipboard text** command:

1.  On your local computer, copy a piece of text to the clipboard.
1.  In IAP Desktop, select **Session > Type clipboard text**.

The **Type clipboard text** command simulates keyboard input and only supports characters supported
by your current keyboard layout. Unsupported characters are replaced with `?`.
    
## :material-message-alert: "Your credentials did not work" when using saved credentials

**Symptom**: You've configured valid credentials, but each time you try to connect to a VM, the _Your credentials did not work_ dialog appears. 
After re-entering the same credentials again, the connection succeeds.

This issue can be the intentional effect of the
[Always prompt for password upon connection :octicons-link-external-16:](https://admx.help/?Category=Windows_10_2016&Policy=Microsoft.Policies.TerminalServer::TS_PASSWORD) 
group policy setting. This policy is configured by default on [CIS hardened images :octicons-link-external-16:](https://www.cisecurity.org/cis-hardened-images/google/).

To mitigate this issue, avoid saving passwords for affected Windows VMs and enter credentials manually instead.

## :material-message-alert: "Your credentials did not work"

**Symptom**: You've configured valid credentials, but each time you try to connect to a VM, the _Your credentials did not work_ 
dialog appears. Re-entering the credentials does not solve the issue.

This behavior can occur if the [LAN Manager authentication level :octicons-link-external-16:](https://docs.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/network-security-lan-manager-authentication-level) of your local workstation and the VM are incompatible.

Because of the way IAP Desktop tunnels RDP connections, it always uses NTLM for authentication and can't use Kerberos. 
Depending on the LAN Manager authentication level configured on both machines, authentication will either use NTLM or NTLMv2.
If you've configured the VM to demand NTLMv2 (authentication level `5`), but your local workstation uses level `1`, `2`, or `3`, protocol 
negotiation fails and your credentials are rejected.

To solve this issue, make sure that the LAN Manager authentication level on both machines is compatible.


## Other errors

If you encounter other error messages, try manually establishing a IAP TCP forwarding tunnel:

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
