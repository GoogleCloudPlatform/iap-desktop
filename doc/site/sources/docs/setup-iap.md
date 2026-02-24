# Configure IAP in your project

## Create a firewall rule

???+ info "Required roles"

    To follow the steps in this section, you need the following roles:
    
    *   [ ] [Compute Admin](https://cloud.google.com/compute/docs/access/iam) or
        [Compute Security Admin](https://cloud.google.com/compute/docs/access/iam) on the project.
        

To allow IAP TCP forwarding connections to VMs, you must configure a firewall rule that:

*   applies to all VM instances that you want to be accessible by using IAP.
*   allows ingress traffic from the IP range `35.235.240.0/20`. This
    range contains all IP addresses that IAP uses for TCP forwarding.
*   allows connections to all ports that you want to be accessible by
    using IAP TCP forwarding, for example, port `22` for SSH and port `3389` for RDP.

To create a firewall rule, do the following:


=== "Console"

    To allow RDP and SSH access to all VM instances in your network, do the following:


    *   Open the Firewall Rules page.

        [Open Firewall Rules](https://console.cloud.google.com/networking/firewalls/list?walkthrough_id=iam--create-firewall-rule){ .md-button }

    *   Select a project.
    *   On the Firewall Rules page, click **Create firewall rule**.
    *   Configure the following settings:

        *   **Name**: <code>allow-ingress-from-iap</code>
        *   **Direction of traffic**: **Ingress**
        *   **Target**: **All instances in the network**
        *   **Source filter**: **IP ranges**
        *   **Source IP ranges**: <code>35.235.240.0/20</code>
        *   **Protocols and ports**: Select **TCP** and enter <code>22,3389</code> to allow both
            RDP and SSH.

    *   Click **Create**.

=== "gcloud"

    To allow RDP access to all VM instances in your network, run:
    
        gcloud compute firewall-rules create allow-rdp-ingress-from-iap \
            --direction INGRESS \
            --action allow \
            --rules tcp:3389 \
            --source-ranges 35.235.240.0/20
    
    For SSH access, run:
    
        gcloud compute firewall-rules create allow-ssh-ingress-from-iap \
            --direction INGRESS \
            --action allow \
            --rules tcp:22 \
            --source-ranges 35.235.240.0/20
    
    For other protocols, run
    
        gcloud compute firewall-rules create allow-ingress-from-iap \
            --direction INGRESS \
            --action allow \
            --rules tcp:PORT \
            --source-ranges 35.235.240.0/20
    
    where <code>PORT</code> is the port used by the protocol.

???+ tip

    We strongly recommend to disable or delete the firewall rules `default-allow-ssh` 
    and `default-allow-rdp`. These [default rules](https://cloud.google.com/vpc/docs/firewalls#default_firewall_rules) 
    allow SSH and RDP connections from all IP addresses, not only from IAP, and might put
    your VM instances at risk.
    
    
## Grant access

To allow users to connect to VM instances using IAP TCP forwarding, you must grant them
the **IAP-secured Tunnel User** role. You can grant this role for individual VMs, a project,
or entire folders.

### Grant access to all VMs in a project

???+ info "Required roles"

    To follow the steps in this section, you need the following roles:
    
    *   [ ] [Project IAM Admin](https://cloud.google.com/iam/docs/understanding-roles) or
        [Security Admin ](https://cloud.google.com/iam/docs/understanding-roles) on the project.
        


To grant a user access access to all VMs in a project, do the following:

=== "Console"

    *   Open the **IAM & Admin** page in the Cloud console.
    
        [Open IAM & Admin](https://console.cloud.google.com/project/_/iam-admin){ .md-button }

    *   On the **IAM & Admin** page, click **Add** and configure the following:
      
        *   **New principals**: Specify the user or group you want to grant access.</li>
        *   **Select a role**: Select **Cloud IAP > IAP-Secured Tunnel User**.</li>
    
    *   Click **Save**.</li>

=== "gcloud"

    Run the following command:

        gcloud projects add-iam-policy-binding PROJECT_ID \
          --member=user:EMAIL \
          --role=roles/iap.tunnelResourceAccessor

    Replace the following:
    
    * <code>PROJECT_ID</code>: ID of the project
    * <code>EMAIL</code>: email address of the user you want to grant access,
      for example `user@example.com`.

### Grant access to a specific VM

???+ info "Required roles"

    To follow the steps in this section, you need the following roles:
    
    *   [ ] [IAP Policy Admin](https://cloud.google.com/iap/docs/managing-access) on the project.
    
To grant a user access to a specific VM, do the following:

=== "Console"

    *   Open the IAP admin page and select the **SSH and TCP Resources** tab.
    
        [Open SSH and TCP Resources](https://console.cloud.google.com/security/iap?tab=ssh-tcp-resources){ .md-button }
    
    *   On the **SSH and TCP Resources** tab of the IAP admin page, select the VM instances
        that you want to configure.
    *   Click **Show info panel** if the info panel is not visible.
    *   Click **Add principal** and configure the following:
    
        *   **New principals**: Specify the user or group you want to grant access.
        *   **Select a role**: Select **Cloud IAP > IAP-Secured Tunnel User**.
      
    *   Click **Save**.

## What's next

*   See how you can [connect to Windows VMs by using Remote Desktop](connect-windows.md)
*   Learn how you can [connect to Linux VMs by using SSH](connect-linux.md)