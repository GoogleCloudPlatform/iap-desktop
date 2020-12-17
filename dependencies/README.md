# Dependencies

This folder contains scripts and configuration to build
dependenices requires by IAP Desktop. The dependencies
are built into NuGet packages and published to a NuGet feed.

## Building

Building the dependencies requires:

* A Nuget feed that you can publish packages to.
* Visual Studio 2019 with C# and C++ support or the equivalent components
  from the Windows SDK
* Internet access to download dependencies  

Steps to build the dependencies requires:

1. Open a command prompt (`cmd.exe`)
1. Register the NuGet feed:

    ```
    nuget source Add -Name "iap-desktop" -Source "https://..." 

    ```
    
    **Note**: The feed must be named `iap-desktop`.

1. Build third-party packages:

    ```
    cd %WORKSPACE%\dependencies
    build
    ```

    You should now have a local Nuget package repository at
    %WORKSPACE%\sources\third_party\NuGetPackages containing
    third-party packages.
    