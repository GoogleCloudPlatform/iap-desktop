# Deploy IAP Desktop automatically

If multiple users in your organizations use IAP Desktop, you can automate the
process of deploying IAP Desktop to users' workstations by using Active Directory or Intune.

=== "Active Directory"

    To use an [Active Directory Group Policy :octicons-link-external-16:](https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/hh831791(v=ws.11))
    object (GPO) to automate IAP Desktop deployments, do the following:

    1.  [Download the IAP Desktop MSI package](https://github.com/GoogleCloudPlatform/iap-desktop/releases)
        and copy it to a file share that is readable by domain users.
    1.  In the **Group Policy Management Console**, create or select a GPO.
    1.  Link the GPO to an organizational unit that contains the users who should be able to use IAP Desktop.
  
        !!! note
            IAP Desktop is installed per-user, not per-computer. Make sure you choose a scope that captures relevant
            users, not computers.
 
    1.  Right-click the GPO and select **Edit**.
    1.  Navigate to **User Configuration > Policies > Software Settings > Software installation**
    1.  In the right window pane, right click on the empty list and select **New > Package**.
         1.  Enter the UNC path to the IAP Desktop MSI package.
         1.  In the **Deploy software** dialog, select **Assigned** and click **OK**.
    1.  Right-click **IAP Desktop** in the list of packages and select **Properties**.
         1.  Switch to the **Deployment** tab.
         1.  Set **Install this application at logon** to **Enabled**.
         1.  Click **Advanced**
         1.  Set **Ignore language when deploying this package** to **Enabled**, then click **OK**.
         1.  Click **OK** to close the properties dialog.
    1.  Close the Group Policy Management Editor window.

=== "Intune"

    To use the Intune Settings Catalog to automate IAP Desktop deployments, do the following:

    1.  Create an empty folder on your local computer.
    1.  [Download the IAP Desktop MSI package](https://github.com/GoogleCloudPlatform/iap-desktop/releases) and 
        save it to the empty folder.
    1.  [Download the Microsoft Win32 Content Prep Tool :octicons-link-external-16:](https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool).
    1.  Open a command prompt and convert the IAP Desktop MSI package into a `.intunewin` file:

        ```
        IntuneWinAppUtil -c FOLDER -s IapDesktopX64.msi -o .
        ```

        Replace `FOLDER` with the path of the folder that contains the IAP Desktop MSI package.

    1.  In the [Intune admin center :octicons-link-external-16:](https://intune.microsoft.com/), go to
        **Apps > Windows.**.
    1.  Click **Add** and configure the following:

        1.  Set **App type** to **Windows app (Win32)**.
        1.  Click **Select**.

    1.  Click **Select app package** file and do the following:

        1.  Select the `.intunewin` file that you created previously
            by running `IntuneWinAppUtil`.
        1.  Click **OK**.

    1.  On the **App information page**, do the following:

        1.  Set **Publisher** to `Google`
        1.  Click **Next**.

    1.  On the **Program** page, click **Next**.
    1.  On the **Requirements** page, do the following:
    
        1.  Set **Operating systema architecture** to **64 bit**.
        1.  Set **Minimum operating system** to **Windows 1607**.
        1.  Click **Next**.

    1.  On the **Detection rules** page, do the following:

        1.  Set **Rules format** to **Manually configure detection rules**.
        1.  Set **Rule type** to **MSI**.
        1.  Click **OK**.
        1.  Click **Next**.

    1.  On the **Dependencies** page, click **Next**.
    1.  On the **Supersedence** page, click **Next**.
    1.  On the **Assignments** page, do the following:

        1.  Select users who should be able to use IAP Desktop.
        1.  Click **Next**.

    1.  Click **Create**.

## What's next

* [Use group policies to customize IAP Desktop](group-policies.md).