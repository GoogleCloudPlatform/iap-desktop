???+ info "Required roles"

    To follow the steps in this guide, you need the following roles:
    
    *   [ ] [Compute Viewer :octicons-link-external-16:](https://cloud.google.com/compute/docs/access/iam) on the project.
    *   [ ] [IAP-Secured Tunnel User :octicons-link-external-16:](https://cloud.google.com/iap/docs/managing-access#roles) on
        the project or VM.
               

???+ success "Prerequisites"

    To follow the steps in this guide, make sure that you meet the following prerequisites:

    *   You run SQL Server on a Compute Engine VM. IAP Desktop currently can't connect to Cloud SQL.
    *   You downloaded and installed [SQL Server Management Studio (SSMS) :octicons-link-external-16:](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) on your computer.
    *   You [created a firewall rule](setup-iap.md) that allows IAP to connect to port <code>1433</code> of your SQL Server VM.

To use SQL Server Management Studio and connect to SQL Server, do the following:

1.  In the **Project Explorer** tool window, right-click your SQL Server VM and select **Connect client application > SQL Server Management Studio**:
    
    ![Context menu](images/Connecting-SQLServer.png)
    
    
1.  IAP Desktop now creates an [IAP TCP forwarding tunnel :octicons-link-external-16:](https://cloud.google.com/iap/docs/using-tcp-forwarding) and
    opens SQL Server Management Studio. 
    
    If your connection settings contain Windows credentials, SQL Server Management Studio automatically 
    authenticates you to SQL Server [using Windows authentication :octicons-link-external-16:](https://learn.microsoft.com/en-us/sql/relational-databases/security/choose-an-authentication-mode#connecting-through-windows-authentication).
    
    If you haven't configured Windows credentials yet, you can enter credentials manually:
    
    ![SSH Terminal](images/Connecting-SQLServer-Credentials.png)
    
    ???+ Note
    
        You can use Windows authentication even if your local computer isn't domain-joined, or
        joined to a different Active Directory domain than the SQL Server VM.

## Customize connection settings

To customize the connection settings, you can use the **Connection Settings** tool window:

1.  In the **Project Explorer** tool window, right-click the SQL Server VM and select **Connection Settings**.
1.  In the **Connection Settings** window, you can save your Windows credentials or switch to
    [SQL Server authentication :octicons-link-external-16:](https://learn.microsoft.com/en-us/sql/relational-databases/security/choose-an-authentication-mode#connecting-through-sql-server-authentication):

    ![Connection settings](images/Connection-Settings-SQL-Server.png)

    If you specify a setting
    that deviates from the default, it is shown in bold typeface.

Instead of customizing settings for each VM instance individually, you can also specify settings that apply to
an entire zone or projects:

1.  In the **Project Explorer** tool window, right-click a zone or project and select **Connection Settings**.
1.  In the **Connection Settings** window, customize settings as needed. The settings apply to all VM instances
    in the respective zone or project, unless explicitly overridden.

## What's next

*   See how you can [connect to Windows VMs by using Remote Desktop](connect-windows.md)
*   Learn how you can [connect to Linux VMs by using SSH](connect-linux.md)