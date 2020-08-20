# IAP Desktop

IAP Desktop is a Windows application that allows you to manage multiple Remote Desktop connections 
to Windows VM instances that run on Google Cloud. 

[<img src="doc/images/download.png">](https://github.com/GoogleCloudPlatform/iap-desktop/releases/latest/download/IapDesktop.msi)

## Overview

IAP Desktop uses 
[Identity-Aware-Proxy TCP tunneling](https://cloud.google.com/iap/docs/tcp-forwarding-overview) to 
connect to VM instances, combining the convenience of a Remote
Desktop connection manager with the security and flexibility of Identity-Aware-Proxy:

* You can connect from anywhere, not only from selected networks
* You can connect to VM instances that not expose RDP publicly or do not have a public IP address
* You can exert fine-grained control which users and which devices should be allowed to access which VM instances

![Screenshot of IAP Desktop](doc/images/iapdesktop-animated-800.gif)



## Learn more about IAP Desktop

* [Setting up IAP-Desktop](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Installation)
* [Connecting to VM instances](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Connecting-to-instances)
* [Connecting to VM instances from within a web browser](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Browser-Integration)
* [Viewing instance details](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Viewing-instance-details)
* [Viewing instance activity](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Viewing-instance-activity)
* [Analyzing VM instance usage](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Analyzing-usage)
* [Troubleshooting](https://github.com/GoogleCloudPlatform/iap-desktop/wiki/Troubleshooting)




_IAP Desktop is an open-source project and not an officially supported Google product._

_All files in this repository are under the
[Apache License, Version 2.0](LICENSE.txt) unless noted otherwise._
