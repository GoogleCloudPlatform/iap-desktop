# escape=`

#
# Copyright 2023 Google LLC
#
# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.
#

FROM mcr.microsoft.com/windows:ltsc2019-amd64 AS desktop

FROM mcr.microsoft.com/windows/servercore:ltsc2019-amd64

#
# Install missing Terminal Server components from desktop image. 
#
# NB. We could use the Desktop image instead, but using the Server Core image
# helps make better use of Kokoro image cache.
#
COPY --from=desktop \Windows\System32\mstscax.dll           \Windows\System32\
COPY --from=desktop \Windows\System32\msacm32.dll           \Windows\System32\
COPY --from=desktop \Windows\System32\en-US\mstscax.dll.mui \Windows\System32\en-US\

COPY --from=desktop \Windows\SysWOW64\mstscax.dll           \Windows\SysWOW64\
COPY --from=desktop \Windows\SysWOW64\msacm32.dll           \Windows\SysWOW64\

RUN c:\Windows\System32\regsvr32.exe c:\Windows\System32\mstscax.dll /s
RUN c:\Windows\SysWOW64\regsvr32.exe c:\Windows\SysWOW64\mstscax.dll /s

#
# Install VS components, adapted from 
# https://github.com/microsoft/dotnet-framework-docker/blob/main/src/sdk/4.8/windowsservercore-ltsc2019/Dockerfile
#
# For a list of workload and component IDs, see
# https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=vs-2022
#
RUN `
    # Install VS 2022 Build Tools
    curl -fSLo vs_BuildTools.exe https://aka.ms/vs/17/release/vs_BuildTools.exe `
    && start /w vs_BuildTools ^ `
        --installPath "%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools" ^ `
        --add Microsoft.Net.Component.4.6.2.SDK ^ `
        --add Microsoft.Net.Component.4.6.2.TargetingPack ^ `
        --add Microsoft.Net.Component.4.7.2.SDK ^ `
        --add Microsoft.Net.Component.4.7.2.TargetingPack ^ `
        --add Microsoft.Net.Component.4.8.SDK ^ `
        --add Microsoft.Net.Component.4.8.TargetingPack ^ `
        --add Microsoft.Net.ComponentGroup.4.6.2-4.7.1.DeveloperTools ^ `
        --add Microsoft.Net.ComponentGroup.4.8.DeveloperTools ^ `
		--add Microsoft.VisualStudio.Component.NuGet ^ `
		--add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 ^ `
		--add Microsoft.VisualStudio.Component.Windows11SDK.22000 ^ `
		--add Microsoft.VisualStudio.ComponentGroup.MSIX.Packaging ^ `
        --add Microsoft.VisualStudio.Workload.VCTools ^ `
		--add Microsoft.VisualStudio.Workload.ManagedDesktop ^ `
		--add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools ^ `
		--add Microsoft.VisualStudio.Workload.NativeDesktop ^ `
		--add Microsoft.VisualStudio.Workload.MSBuildTools ^ `
        --includeRecommended --quiet --norestart --nocache --wait `
    && powershell -Command "if ($err = dir $Env:TEMP -Filter dd_setup_*_errors.log | where Length -gt 0 | Get-Content) { throw $err }" `
    && del vs_BuildTools.exe `
    `
    # Cleanup
    && (for /D %i in ("%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\*") do rmdir /S /Q "%i") `
    && (for %i in ("%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\*") do if not "%~nxi" == "vswhere.exe" del "%~i") `
    && powershell Remove-Item -Force -Recurse "%TEMP%\*" `
    && rmdir /S /Q "%ProgramData%\Package Cache"

ENV chocolateyVersion=1.4.0
RUN powershell -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))"
RUN choco install git -y --no-progress

#TODO: add gcloud
#TODO: add cygwin?

#
# Set PATH.
#
RUN powershell setx /M PATH $(${Env:PATH} `
    + \";${Env:ProgramFiles}\NuGet\" `
    + \";${Env:ProgramFiles}\Git\bin\" `
    + \";${Env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\" `
    + \";${Env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\" `
    + \";${Env:ProgramFiles(x86)}\Microsoft SDKs\ClickOnce\SignTool\")

#
# Initialize Kokoro environment.
#
RUN setx /M CYGWIN winsymlinks:native `
    && setx /M HOME C:\Users\ContainerAdministrator\ `
    && setx /M HOMEDRIVE C: `
    && setx /M HOMEPATH \Users\ContainerAdministrator\ `
    && setx /M KOKORO_POSIX_ROOT /tmpfs `
    && setx /M KOKORO_ROOT C:\tmpfs\ `
    && setx /M SHELL /bin/bash
	
SHELL ["powershell", "-NoLogo", "-ExecutionPolicy", "Bypass", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]