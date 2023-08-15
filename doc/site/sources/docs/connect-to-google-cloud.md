---
Diagrams: https://docs.google.com/presentation/d/1M3uXC6tJQDtxIO89fO7tBxXumQnbfIb4jf9v7dHMGE4/edit#slide=id.p
---

# Connect to Google Cloud

## Connect via public internet

![Connect via public internet](images/connect-via-internet.png)

If your local network requires clients to connect to the internet through
a proxy server...


## Connect through Private Service Connect

![Connect through Private Service Connect](images/connect-via-psc.png)

*  Connections to IAP and other Google APIs traverse the Cloud Interconnect/VPN link,
   bypassing any local proxy server.
*  ..might still need proxy for external IdP

## Connect through Private Google Access


