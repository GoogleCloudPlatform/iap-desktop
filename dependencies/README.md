# Dependencies

This folder contains scripts and configuration to build
dependencies requires by IAP Desktop. The dependencies
are built into NuGet packages and published to a NuGet feed.

## Building

Building the dependencies requires:

* Visual Studio 2019 with C# and C++ support or the equivalent components
  from the Windows SDK
* Internet access to download dependencies  

Steps to build the dependencies requires:

1. Open a command prompt (`cmd.exe`)
1. Build dependency packages:

    ```
    cd %WORKSPACE%\dependencies
    build
    ```

    You should now have a local Nuget package repository at
    %WORKSPACE%\dependencies\NuGetPackages containing
    the dependency packages. 
    
    The repository is automatically registered with NuGet.
