# Building

## Prerequisites

Building IAP Desktop requires:

* Visual Studio 2019 with C# and C++ support or the equivalent components
  from the Windows SDK
* Internet access to download dependencies  
  
## Building

1. Open a command prompt (`cmd.exe`)
1. Build third-party packages:

    ```
    cd %WORKSPACE%\sources\third_party
    build
    ```

    You should now have a local Nuget package repository at
    %WORKSPACE%\sources\third_party\NuGetPackages containing
    third-party packages.

1. Build the main solution:

    ```
    cd %WORKSPACE%\sources
    build installer
    ```

    You should now find an MSI package at %WORKSPACE%\sources\installer\bin.