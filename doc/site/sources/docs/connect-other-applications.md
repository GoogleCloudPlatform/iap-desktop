# Access server applications

???+ info "Required roles"

    To follow the steps in this guide, you need the following roles:
    
    *   [ ] [Compute Viewer :octicons-link-external-16:](https://cloud.google.com/compute/docs/access/iam) on the project.
    *   [ ] [IAP-Secured Tunnel User :octicons-link-external-16:](https://cloud.google.com/iap/docs/managing-access#roles) on
        the project or VM.
     
???+ success "Prerequisites"

    To follow the steps in this guide, make sure that you meet the following prerequisites:

    *   You [created a firewall rule](setup-iap.md) that allows IAP to connect to the port used by the server application.
          

You can use IAP Desktop to access server applications in two ways:

1.  You can let IAP Desktop launch and [connect a client application](#connect-the-database-client)
    for you. IAP Desktop automatically establishes an
    [IAP TCP forwarding tunnel :octicons-link-external-16:](https://cloud.google.com/iap/docs/using-tcp-forwarding)
    and keeps the tunnel open until you close the client application.

    This is the most convenient option, but it only works for client applications that allow
    connection details (server name, port number) to be passed as a command line parameter.

1.  You can let IAP Desktop [open a tunnel](#open-a-tunnel). You can then use any tool to
    connect to that tunnel and the tunnel remains open until you close IAP Desktop.

    This option is slightly less convenient, but works with most client applications.

## Connect a client application

To launch and connect a client application automatically, do the following:

=== "Chrome"


    1.  In the **Project Explorer** tool window, right-click your web server VM and select 
        **Connect client application > Chrome (port 80)** or **Chrome (port 8080)**.

    1.  IAP Desktop now creates an [IAP TCP forwarding tunnel :octicons-link-external-16:](https://cloud.google.com/iap/docs/using-tcp-forwarding) and
        launches an instance of Chrome in guest mode.
    
=== "Custom"

    You can register your own client applications by 
    [creating an IAP Application Protocol Configuration (IAPC)](client-application-configuration.md).


## Open a tunnel

You can let IAP Desktop open a tunnel and connect to the tunnel by doing the following:

=== "Server port"

    1.  In the **Project Explorer** tool window, right-click your database VM and select 
        **Tunnel to > Other server port**.
    1.  On the **Forward local port** dialog, enter the port number to connect to.
    1.  Click **OK**:

        A notification appears:

        ![Baloon notification](images/access-server-baloon.png){ width="300" }

    You can now use any client application to connect to the forwarded port.

=== "Custom"

    You can register your own client applications by 
    [creating an IAP Application Protocol Configuration (IAPC)](client-application-configuration.md).


To view all active tunnels and their port numbers, select **View > Active IAP tunnels** in the main menu.


!!! note

    When you open a tunnel to the same VM again in the future, IAP Desktop
    will use the same port number unless it's in use by a different application.

