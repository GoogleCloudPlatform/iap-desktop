# SSH algorithms and keys

## Public key authentication

IAP Desktop supports the following algorithms for public key authentication:

| Key type         | Default          | Algorithm                         |
| ---------------- | ---------------- | --------------------------------- |
| RSA (3072 bit)   |                  | `rsa-sha2-512` or `rsa-sha2-256`  |
| ECDSA NIST P-256 |                  | `ecdsa-sha2-nistp256`             |
| ECDSA NIST P-384 | :material-check: | `ecdsa-sha2-nistp384`             |
| ECDSA NIST P-521 |                  | `ecdsa-sha2-nistp521`             |

By default, IAP Desktop uses `ecdsa-sha2-nistp384` for public key authentication.
To use a different algorithm, go to **Options > SSH > Public key authentication**
and change the key type.


## SSH keys

IAP Desktop uses the 
[Microsoft Software Key Storage Provider :octicons-link-external-16: ](https://docs.microsoft.com/en-us/windows/win32/seccertenroll/cng-key-storage-providers#microsoft-software-key-storage-provider)
to store SSH keys and configures private keys to be non-exportable.
You can list existing keys by running the following command:

    certutil -csp "Microsoft Software Key Storage Provider" -key -user | findstr IAPDESKTOP_

IAP Desktop maintains at most one key per key type and Google/workforce identity federation user.

### Ephemeral keys

Optionally, you can configure IAP Desktop to use an ephemeral SSH key:

1.  Go to **Options > SSH > Public key authentication**
2.  Set **Use persistent key and store it in Windows CNG key store** to **disabled**

When you use this configuration, IAP Desktop generates a new SSH key pair every time 
you launch it, and it won't store the private key anywhere.