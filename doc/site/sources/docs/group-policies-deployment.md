# Use group policies to deploy IAP Desktop automatically

IAP Desktop is distributed as a Windows Installer package (`.msi`). To automatically
roll out IAP Desktop to multiple workstations, you can use a software configuration 
management tool such as System Center or you can configure a
[Active Directory Group Policy :octicons-link-external-16:](https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/hh831791(v=ws.11)).
object (GPO).

## Distribute IAP Desktop using a GPO

You can use a group policy object (GPO) to automatically install IAP Desktop for
your users:

1.  Download the IAP Desktop MSI package and copy it to a file share that is readable by domain users.
1.  In the **Group Policy Management Console**, create or select a GPO.
1.  Link the GPO to an organizational unit that contains the users who should be able to use IAP Desktop.

    Note: IAP Desktop is installed per-user, not per-computer. The scope must be configured so that it
    captures relevant users, not computers.

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

???+ Note

    If you distribute IAP Desktop by using group policy, it's best to disable automatic updates. See
    next section for details.

## What's next

* [Use group policies to customize IAP Desktop](group-policies.md).