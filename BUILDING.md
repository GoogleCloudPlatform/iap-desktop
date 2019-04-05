# Building

## Building the plugin

The plugin is written in C#. Building the plugin requires:

* Visual C# 2017.
* [Microsoft Remote Desktop Connection Manager](https://www.microsoft.com/en-us/download/details.aspx?id=44989) 

If you open the solution for the first time, Visual Studio might report a broken 
reference to `RDCMan` if it cannot find `RDCMan.exe` on disk. This happens if
you have installed RDCMan to a location other than 
`C:\Program Files (x86)\Microsoft\Remote Desktop Connection Manager\`.
To fix this, remove and re-create the reference, pointing Visual Studio to the
location of `RDCMan.exe`.

## Building the Windows Installer package

The Windows Installer package is built using [WiX Toolset](https://wixtoolset.org/) and requires the following
additional packages to be installed on your system:
* [WiX toolset v4](https://wixtoolset.org/releases/v4-0-0-5205/)
* [WiX Toolset Visual Studio 2017 Extension](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension)