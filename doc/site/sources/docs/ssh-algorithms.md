# SSH algorithms

## Public key authentication

IAP Desktop supports the following algorithms for public key authentication:

| Key type         | Default    | Algorithm                         |
| ---------------- | ---------- | --------------------------------- |
| RSA (3072 bit)   |            | `rsa-sha2-512` or `rsa-sha2-256`  |
| ECDSA NIST P-256 |            | `ecdsa-sha2-nistp256`             |
| ECDSA NIST P-384 | Default    | `ecdsa-sha2-nistp384`             |
| ECDSA NIST P-521 |            | `ecdsa-sha2-nistp521`             |

By default, IAP Desktop uses `ecdsa-sha2-nistp384` for public key authentication.
To use a different algorithm, go to **Options > SSH > Public key authentication**
and change the key type.

Keys are created automatically on first use, and managed using the
[Microsoft Software Key Storage Provider :octicons-link-external-16:](https://docs.microsoft.com/en-us/windows/win32/seccertenroll/cng-key-storage-providers#microsoft-software-key-storage-provider).