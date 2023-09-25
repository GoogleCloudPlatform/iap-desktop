# Use group policies to manage IAP Desktop

You can centrally manage IAP Desktop by using 
[Active Directory Group Policies :octicons-link-external-16:](https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/hh831791(v=ws.11)).

## Distribute IAP Desktop to workstations

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


## Customize IAP Desktop

You can use a group policy object (GPO) to configure policies for IAP Desktop. Policies take
precendence of user settings: When you configure a policy, users can't change the respectice
setting anymore. 

For a full list of settings that you can control using group policies, see [Group policy reference](group-policy-reference.md).

To configure policies, you first have to install the IAP Desktop Policy Templates:

1.  Download the `PolicyTemplates` package from the [downloads page](https://github.com/GoogleCloudPlatform/iap-desktop/releases).
1.  Extract the package int the `PolicyDefinitions` folder of your 
    [central store :octicons-link-external-16:](https://docs.microsoft.com/en-us/troubleshoot/windows-server/group-policy/create-central-store-domain-controller).

You can now use the IAP Desktop Policy Templates to configure policies:

1.  In the **Group Policy Management Console**, create or select a GPO.
1.  Link the GPO to an organizational unit that contains the users who should be able to use IAP Desktop.

    Note: You can configure policies per-computer or per-user. Computer-based policies take precendence
    over user-based policies.

1.  Right-click the GPO and select **Edit**.
1.  Navigate to **User (or Computer) Configuration > Policies > Administrative Templates > Google IAP Desktop**
    and customize policies as necessary.

    ![Policies](images/Policies.png)

1.  Close the Group Policy Management Editor window.


## Customize server-side policies

To disallow clipboard sharing or restrict the usage of other Remote Desktop features, 
configure [group policies :octicons-link-external-16:](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-remotedesktopservices)
on the target VM instance. You can configure these policies either by using the 
[Local Group Policy Editor](https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2012-r2-and-2012/dn265982(v=ws.11))
or by using Active Directory to apply a group policy.

You can find Remote Desktop policies under 
**User (or Computer) Configuration > Policies > Administrative Templates > Windows Components > Remote Desktop Services > Remote Desktop Session Host**.