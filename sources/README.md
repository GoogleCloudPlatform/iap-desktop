# IAP Desktop sources

This folder contains the source code of IAP Desktop.

## Building IAP Desktop

Before you can build IAP Desktop, you must build the `/dependencies` folder
first.

Building IAP Desktop requires:

* Visual Studio 2019 with C# and C++ support or the equivalent components
  from the Windows SDK
* Internet access to download dependencies  
  
Steps to build IAP Desktop:

1. Open a command prompt (`cmd.exe`)
1. Build the application and installer:

    ```
    cd %WORKSPACE%\sources
    build installer
    ```

    You should now find an MSI package at %WORKSPACE%\sources\installer\bin.