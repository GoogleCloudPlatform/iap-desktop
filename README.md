# IAP Desktop

IAP Desktop is a Remote Desktop and SSH client that lets you connect to your Google Cloud VM instances from anywhere.

[<img src="doc/images/download.png">](https://github.com/GoogleCloudPlatform/iap-desktop/releases/latest/download/IapDesktop.msi)

<sub>
For Windows 11/10/8.1. No admin rights required.
</sub>

## Access Linux and Windows VMs from anywhere

<a href='doc/images/Screenshot_1400.png?raw=true'>
<img src='doc/images/Screenshot_350.png' align='right'>
</a>

IAP Desktop uses [Identity-Aware-Proxy (IAP)](https://cloud.google.com/iap/docs/tcp-forwarding-overview) to connect to VM instances so that you can:

*   Connect to VM instances that don’t have a public IP address
*   Connect from anywhere over the internet

Together, IAP Desktop and [Identity-Aware-Proxy (IAP)](https://cloud.google.com/iap/docs/tcp-forwarding-overview) let you apply zero-trust security to your VMs:

*   Apply fine-grained access controls that define [who can access which VM](https://cloud.google.com/iap/docs/using-tcp-forwarding#configuring_access_and_permissions)
*   Use [access levels](https://cloud.google.com/iap/docs/cloud-iap-context-aware-access-howto) to restrict access by time or location
*   Use [BeycondCorp Enterprise](https://cloud.google.com/beyondcorp-enterprise) to limit access to trusted devices

The application automatically manages IAP TCP tunnels for you, and protects them so that no other users or programs can access them.

<img src='doc/images/pix.gif' width='100%' height='1'>

## Connect to Windows VMs with Remote Desktop

<a href='doc/images/RemoteDesktop_1400.gif?raw=true'>
<img src='doc/images/RemoteDesktop_350.png' align='right'>
</a>

IAP Desktop is a [full-featured Remote Desktop client](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Connecting-to-instances) that lets you:

*   Use multiple Remote Desktop sessions at the same time
*   Switch between full-screen and tabbed Remote Desktop sessions
*   Upload and download files over SFTP

To help you authenticate to Windows VMs, IAP Desktop can:

*   Automatically generate Windows credentials by using the Compute Engine guest agent environment
*   Encrypt and store your Windows credentials locally

:arrow_forward: [Show screencast](doc/images/RemoteDesktop_1400.gif?raw=true)

<img src='doc/images/pix.gif' width='100%' height='1'>

## Connect to Linux VMs with SSH

<a href='doc/images/SSH_1400.gif?raw=true'>
<img src='doc/images/SSH_350.png?raw=true' align='right'>
</a>

IAP Desktop [includes an SSH client and terminal](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Connecting-to-linux-instances) so that you can:

*   Use multiple SSH sessions in parallel, and switch between them using tabs
*   Upload and download files using SFTP

To help you authenticate to Linux VMs, IAP Desktop can:

*   Automatically create and publish SSH keys to [OS Login](https://cloud.google.com/compute/docs/oslogin) or [metadata](https://cloud.google.com/compute/docs/connect/add-ssh-keys#metadata)
*   Use OS Login [2-factor authentication](https://cloud.google.com/compute/docs/oslogin/set-up-oslogin)
*   Store our SSH keys locally using Windows CNG

:arrow_forward: [Show screencast](doc/images/SSH_1400.gif?raw=true)

<img src='doc/images/pix.gif' width='100%' height='1'>


## Manage VMs across projects

<a href='doc/images/Manage_1400.gif?raw=true'>
<img src='doc/images/Manage_350.png?raw=true' align='right'>
</a>

IAP Desktop gives you a consolidated view of your VMs and lets you:

*   Connect to VMs across multiple projects and Google Cloud organizations
*   [View diagnostics information](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Viewing-instance-activity)
    such as logs and serial port output
*   Perform common operations such as starting or stopping VMs


:arrow_forward: [Show screencast](doc/images/Manage_1400.gif?raw=true)

<img src='doc/images/pix.gif' width='100%' height='1'>

## Connect to SQL Server and other services


<a href='doc/images/Client_700.png?raw=true'>
<img src='doc/images/Client_350.png?raw=true' align='right'>
</a>

You can use IAP Desktop to let client applications connect to your Google Cloud VMs through IAP:
Right-click a VM, select the application to launch, and IAP Desktop automatically connects the
application through an IAP TCP forwarding tunnel. Supported client applications include:

*   SQL Server Management Studio (supporting Windows authentication and SQL Server authentication)
*   MySQL Shell
*   Chrome (to connect to management portals and other internal websites)
*   [Custom applications](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Client-application-configuration)

<img src='doc/images/pix.gif' width='100%' height='1'>

## Learn more about IAP Desktop

* [Setting up IAP-Desktop](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Installation)
* [Connecting to Windows VMs](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Connecting-to-instances)
* [Connecting to Linux VMs](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Connecting-to-linux-instances)
* [Connecting to VM instances from within a web browser](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Browser-Integration)
* [Viewing instance details](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Viewing-instance-details)
* [Viewing instance activity](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Viewing-instance-activity)
* [Analyzing VM instance usage](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Analyzing-usage)
* [Managing IAP Desktop using group policies](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Managing-IAP-Desktop-using-group-policies)
* [Troubleshooting](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Troubleshooting)




_IAP Desktop is an open-source project and not an officially supported Google product._

_All files in this repository are under the
[Apache License, Version 2.0](LICENSE.txt) unless noted otherwise._
