# Troubleshooting installation issues

## :material-message-alert: "You need administrative privileges to install IAP Desktop on Windows Server"

On Windows Server, the [DisableUserInstalls :octicons-link-external-16:](https://docs.microsoft.com/en-us/windows/win32/msi/disableuserinstalls)
policy disables per-user installations by default. To install IAP Desktop on Windows Server, use one of the following options:<br><br>

### A. Install as elevated user

You can sidestep the DisableUserInstalls policy by launching the installer as an elevated user:

1. Start an elevated command prompt
1. Launch the installer: `msiexec /i "IapDesktop.msi"`


### B. Change the DisableUserInstalls policy

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

### C. Performing an administrative installation

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
