#  Troubleshooting SSH issues

## :material-message-alert: Windows CNG key container or key store is inaccessible

**Symptom**: When trying to connect to a Linux VM, you're seeing one of the following error messages:

* _Creating or opening the SSH key failed because the Windows CNG key container or key store is inaccessible_
* _Failed to create or access cryptographic key_

IAP Desktop uses a public/private key pair for SSH authentication and uses the
[Windows CNG key store :octicons-link-external-16:](https://learn.microsoft.com/en-us/windows/win32/seccng/key-storage-and-retrieval)
to securely store the key in your Windows profile.

The error messages above indicate that IAP Desktop was unable to create or access the SSH key 
in the CNG key store. There are multiple reasons why this can happen:

1.  The file system permissions of your key store are corrupt. This is typically the case when the error details
    include the text _Object already exists_.
    
    To resolve this issue, try [switching key types](#switch-key-types), [deleting the key](#delete-the-key),
    or [using an ephemeral key](#use-an-ephemeral-key).

1.  You've logged in using a [temporary or mandatory Windows profile :octicons-link-external-16:](https://learn.microsoft.com/en-us/windows/win32/shell/mandatory-user-profiles).
    This is typically the case when the error details include the text 
    _The profile for the user is a temporary profile_.
    
    When you use a temporary or mandatory Windows profile, the Windows CNG key store is read-only.
    
    To resolve this issue, log in using a regular Windows profile or [use an ephemeral key](#use-an-ephemeral-key).

### Switch key types

IAP Desktop automatically creates a public/private key pair for SSH authentication on first use. The
type of key it creates depends on what you configured under **Tools > Options > SSH**. 

You can force IAP Desktop to create a new key by going to **Tools > Options > SSH** and switching
the key type to a value you haven't used before, for example from _ECDSA NISTP-384_ to _ECDSA NISTP-521_.

### Delete the key

You can manually delete the CNG keys used by IAP Desktop by doing the following:

1.  Open a command prompt. The command prompt doesn't need to be elevated.
1.  Use `certutil` to list your keys:

        certutil -csp "Microsoft Software Key Storage Provider" -key -user | findstr IAPDESKTOP_
        
1.  Find all entries prefixed `IAPDESKTOP_`. For example:

        IAPDESKTOP_bob@example.com_00000012_094FE673
        cbb967c9dd228c77fcc7cccccc2ee292c_f430a14c-aaaa-bbbb-cccc-dddddddddddd
        ECDSA_P384
        ECDSA
        
    The second line of each entry contains the key container name:
    
        cbb967c9dd228c77fcc7cccccc2ee292c_f430a14c-aaaa-bbbb-cccc-dddddddddddd
        
1.  Open the folder `%APPDATA%\Microsoft\Crypto\Keys` and find the file
    named after the key container name. This file contains the encrypted key.

1.  Rename (or delete) the file.
     
The next time you connect to a Linux VM, IAP Desktop will automatically create a new key.

### Use an ephemeral key

!!! note

    This feature requires IAP Desktop 2.39

Instead of using a persistent key for SSH authentication, you can let IAP Desktop use an ephemeral key.
Ephemeral keys are only kept in memory and deleted once you close the application.

To use ephemeral keys, do the following:

1.  Go to **Tools > Options > SSH**.
1.  Disable the option **Use persistent key**.
1.  Click **OK**.

When you use an ephemeral key and IAP Desktop publishes the public key to OS Login or metadata, it 
automatically limits the lifetime of the key to one day.