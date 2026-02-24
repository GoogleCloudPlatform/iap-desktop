# Join a VM to Active Directory

???+ info "Required roles"

    To follow the steps in this guide, you need the following roles:
    
    *   [ ] [Compute Instance Admin (v1) :octicons-link-external-16:](https://cloud.google.com/compute/docs/access/iam) on the project or VM.
    *   [ ] [Service Account User :octicons-link-external-16:](https://cloud.google.com/iam/docs/service-accounts-actas) on the VM's service account (if the VM has an attached service account).

IAP Desktop lets you remotely join a Windows VM to Active Directory without using Remote Desktop.

To remotely join a Windows VM, IAP Desktop performs the following steps:

1.  **First restart**: IAP Desktop temporarily replaces the VM's startup script in metadata and restarts the VM.
2.  **Key generation**: During startup, the script generates an ephemeral RSA key pair and publishes the public 
	key to the VM's serial port.
3.  **Password encryption**: IAP Desktop reads the public key from the serial port, encrypts the Active Directory 
	credentials you provide, and writes the encrypted credentials to the VM's metadata.
4.  **Domain join**: The startup script reads the encrypted credentials from the metadata, decrypts them using 
	its private key, and performs the domain join operation.
5.  **Second restart**: Upon successfully joining the domain, the startup script restarts the VM a second time to
	apply the changes. After the process completes, IAP Desktop restores the VM's original startup scripts.

Encrypting the credentials with an ephemeral key helps ensure that your 
Active Directory credential remains secure and is never exposed in plaintext in the VM's metadata.

## Join a VM to Active Directory

To join a Windows VM to an Active Directory domain, do the following:

1.  In the **Project Explorer** tool window, right-click your Windows VM and select **Control > Join to Active Directory**.

1.  In the **Join to Active Directory** dialog, enter the following information:
    *   **Domain**: The name of the Active Directory domain you want to join.
    *   **Computer name**: The computer name for the VM in Active Directory.
	
1.  Click **Restart and join** and enter credentials for an Active Directory account with permissions to join computers to the domain.

IAP Desktop now performs the domain join operation. Because the VM is restarted twice during the process, it might 
take a few minutes to complete. You can track the progress of the operation in the status bar.
