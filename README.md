# Google Cloud IAP for Remote Desktop

Exposing RDP over the public internet is a security risk and not recommeneded. If you have
set up [hybrid connectivity](https://cloud.google.com/hybrid-connectivity/) to your local network,
then you can securely access your Windows instances over the private network without having 
to expose RDP to the internet.

A more flexible option which works regardless of your location and network is
to use [Cloud IAP TCP forwarding](https://cloud.google.com/iap/docs/tcp-forwarding-overview):
Cloud IAP TCP forwarding works like fully managed bastion host and allows you to create TCP 
tunnels from your local machine to resources running inside your VPC. By creating a tunnel to port 
3389 of a Windows instance running on GCP, you can securely connect to a Remote Desktop from anywhere 
without having to expose any ports to the internet. At the same time, administrators have 
fine-grained control over who is permitted to create tunnels to which resources.

![Architecture](doc/images/Architecture.svg)

Cloud IAP TCP forwarding works for any ports and protocols, which makes the service very versatile.
If you want to use RDP to connect to multiple servers, then this versatility can cause TCP forwarding
to become inconvenient to use as you have to manually set up multiple tunnels before you can open 
Remote Desktop connections.

This repository contains tools that simplify using Cloud IAP TCP forwarding for Remote Desktop 
so that you can use it to securely connect to your servers on an everyday basis.

## Plugin for Microsoft Remote Desktop Connection Manager

Microsoft Remote Desktop Connection Manager (RDCMan) is a popular tool for managing Remote Desktop
connections to fleets of servers. The Cloud IAP plugin extends RDCMan by the ability to remotely
connect to servers via a Cloud IAP TCP forwarding tunnel. 

![Context Menu](doc/images/ContextMenu.png)

If you click _Connect to server via Cloud IAP_, the plugin automatically creates a TCP tunnel for 
you in the background using your local [GCloud credentials](https://cloud.google.com/sdk/docs/authorizing)
and connects you to the remote desktop of the server. The feature makes connecting to a VM instance 
via Cloud IAP TCP forwarding just as convenient as connecting directly, but without the security risk 
of having to expose any ports to the internet.

## Prerequisites

* You have to have the Cloud SDK installed. If you have not, 
  [download and install it first](https://cloud.google.com/sdk/docs/downloads-interactive).
* [Microsoft Remote Desktop Connection Manager](https://www.microsoft.com/en-us/download/details.aspx?id=44989) 
  2.7 must be installed
* .NET Framework (same as RDCMan)

## Installation

1. Download and run the MSI installer. The installer will install the necessary DLLs into the 
   installation folder of RDCMan (Usually, that is 
  `C:\Program Files (x86)\Microsoft\Remote Desktop Connection Manager\` but the installer
   will automatically detect if you have installed RDCMan to a different location). 
2. Restart RDCMan.
3. Done.

## Granting Cloud IAP access to your VM instances

### Firewall rules

To enable Cloud IAP to establish a TCP tunnel between your local workstation and a VM instance,
Cloud IAP has to be able to access the RDP port of your VM instance. By default, the VPC firewall rules
do not permit such access. You therefore have to create a firewall rule that allows Cloud IAP to access
port 3389 (the RDP port) of all relevant instances.

When creating a firewall rule, you can rely on the fact that Cloud IAP 
[always uses a source IP from the range](https://cloud.google.com/iap/docs/using-tcp-forwarding) 
`35.235.240.0/20`.

To allow Cloud IAP to access the RDP port of all VM instances in your VPC, run the following
command in Cloud Shell:

```
gcloud compute firewall-rules create allow-rdp-ingress-from-iap \
    --direction=INGRESS \
    --action=allow \
    --rules=tcp:3389 \
    --source-ranges=35.235.240.0/20 \
    --network=[YOUR-VPC]
```

Replace `[YOUR-VPC]` by the name of your VPC network.

Although `35.235.240.0/20` looks like an external IP address range, Cloud IAP will access your VM over
its internal IP address. VM instances do not need an external IP address for Cloud IAP
TCP tunneling to work.

### IAM policies

By default, only _Project Owners_ are allowed to use Cloud IAP TCP tunneling to access 
VM instances. To allow other users to use Cloud IAP TCP tunneling, adjust the IAM policy 
of your project:

1. In the Cloud Console, go to **Security** > **Identity-Aware Proxy**
2. Select the **SSH and TCP forwarding** tab
3. Select the servers you want to grant access to
4. In the info panel, add the user and assign the **IAP-secured Tunnel User** role.




## Using the plugin

1. In RDCMan, select **File** > **New...** in the main menu to create a new server group. 
2. Name the file `[PROJECT-ID].rdg` where `[PROJECT-ID]` is the name of the GCP project
   containing the VM instances you want to connect to, for example, `my-gcp-project-123.rdg`. 
   Save the file anywhere you like.
3. Right-click the file in the server tree and click _Add GCE instances from [PROJECT-ID]_. This 
   automatially add servers to the server tree for each VM instance in your project. Of course,
   you can also add servers manually as long as their name matches their instance name in GCE.
4. Righ-click a server and click _Connect server via Cloud IAP_. This works like _Connect server_,
   but it will establish the connection via a Cloud IAP TCP tunnel. Therefore, this option will 
   work even if you are in a public network.

All existing RDCMan features are still available and you can choose whether to connect via Cloud IAP
or directly on a server-by-server basis.

Additional features exposed in the context menu:
* _Generate Windows logon credentials_ 
  lets you create a user and [generates a password for a Windows VM instance](https://cloud.google.com/compute/docs/instances/windows/creating-passwords-for-windows-instances) 
  and saves the credentials in RDCMan. If a Windows account does not exist on the instance yet, 
  the command will create a new local Administrator account, otherwise it will reset the
  password of the existing account. 
  This feature requires the _Compute Engine Admin_ role in GCP.
* _Show serial port output_ tails the [serial port output](https://cloud.google.com/compute/docs/instances/viewing-serial-port-output)
  of an instance. This feature allows you to observe the boot process or analyze startup issues.
* _Open in Cloud Console_ takes you to the instance details in the Cloud Console.
* _Show Stackdriver logs_ takes you to the logs of the VM instance.


## Troubleshooting

If you have trouble using _Connect server via Cloud IAP_ function, try manually establishing
a Cloud IAP TCP forwarding tunnel:

1. Open a command prompt window (`cmd.exe`).
2. Run the following command: `gcloud compute start-iap-tunnel [INSTANCE_NAME] 3389 
  --project=[PROJECT] --zone=[ZONE] --local-host-port=localhost:13389`
   Replace `[INSTANCE_NAME]` by the name of an instance and `[PROJECT]` and `[ZONE]` by 
   the project and zone the instance is located in.
4. Wait for the output `Listening on port [13389].` to appear.
3. Launch `mstsc.exe` and try to connect to `localhost:13389`.

If establishing the tunnel does not work, check if a local firewall is blocking `gcloud`
from binding to a local port or blocking communication with Cloud IAP.

## Building the plugin

To build the plugin, check out the [prerequisites](BUILDING.md).

## Privacy

This plugin accesses Google Cloud Platform in order to:

* establish Cloud IAP TCP tunnels to VM instances
* list VMs and obtain metadata and logs for VM instances  
* generate Windows logon credentials if requested

The plugin uses your local installation of [gcloud](https://cloud.google.com/sdk/gcloud/) 
as well as the following APIs for this purpose:

* [Compute Engine API](https://cloud.google.com/compute/docs/reference/rest/v1/)
* [Google OAuth](https://developers.google.com/identity/protocols/OAuth2)

Authentication is performed using your locally saved gcloud credentials.

The plugin does not disclose or transmit any user data to APIs other than the
ones listed above.

## Support

This is not an officially supported Google product.

## License

All files in this repository are under the
[Apache License, Version 2.0](LICENSE.txt) unless noted otherwise.