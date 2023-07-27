# Command Line Options

IAP Desktop supports the following command line options:

| Switch | Synopsis |
| ------ | -------- |
| `/url URL` | Launch IAM Desktop and connect to instance specified by `URL`. For details on the supported URL format, see [Connecting to VM instances from within a web browser](Browser-Integration) |
| `/debug`     |  Launch IAM Desktop with debug logging enabled. Logs are saved to `%APPDATA%\Google\IAP Desktop\Logs`.
| `/profile NAME` | Use profile `NAME` instead of the default profile. |

IAP Desktop maintains a single instance per profile. If you launch a second instance
of IAP Desktop, the first instance will be activated and brought to the front. 