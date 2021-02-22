# IAP Desktop sources

This folder contains the source code of IAP Desktop.

## Building IAP Desktop

Before you can build IAP Desktop, you must build the `/dependencies` folder
first.

Building IAP Desktop requires:

* Visual Studio 2019 with C# and C++ support or the equivalent components
  from the Windows SDK
* Internet access to download dependencies  
* An OAuth Client ID

Steps to obtain an OAuth Client ID:

1. [Create a new internal consent screen](https://console.cloud.google.com/apis/credentials/consent) for the
   scope `.../auth/cloud-platform`. 
1. [Create new OAuth client ID credentials](https://console.cloud.google.com/apis/credentials) and select the
   application type **Desktop App**.
1. Make a copy of `sources/Google.Solutions.IapDesktop/OAuthClient.cs.default`, save it 
   as `sources/Google.Solutions.IapDesktop/OAuthClient.cs` and insert the client credentials.

Steps to build IAP Desktop:

1. Open a command prompt (`cmd.exe`)
1. Build the application and installer:

    ```
    cd %WORKSPACE%\sources
    build installer
    ```

    You should now find an MSI package at %WORKSPACE%\sources\installer\bin.