# IAP Desktop

IAP Desktop is a Windows application that allows you to manage multiple Remote Desktop and SSH connections 
to VM instances that run on Google Cloud. 

[<img src="doc/images/download.png">](https://github.com/GoogleCloudPlatform/iap-desktop/releases/latest/download/IapDesktop.msi)

## Overview

IAP Desktop uses 
[Identity-Aware-Proxy TCP tunneling](https://cloud.google.com/iap/docs/tcp-forwarding-overview) to 
connect to VM instances, combining the convenience of a Remote
Desktop connection manager with the security and flexibility of Identity-Aware-Proxy:


* You can connect from anywhere, not only from selected networks.
* You can connect to VM instance that do not have a public IP address or NAT access to the internet.
* Because the TCP forwarding tunnel is established over HTTPS, you can connect even if your workstation
  is behind a corporate firewall or proxy.
* You can control who should be allowed to connect to a VM in a fine-grained manner by using 
  [Cloud IAM](https://cloud.google.com/iap/docs/using-tcp-forwarding#configuring_access_and_permissions).
* You do not need to expose SSH or RDP over the public internet. 

![Screenshot of IAP Desktop](doc/images/iapdesktop-animated-800.gif)



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
