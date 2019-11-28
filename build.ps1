#
# Copyright 2019 Google LLC
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

$ErrorActionPreference = "stop"

# Product version to be used for MSI (2 digit).
$ProductVersion="1.1"

$Msbuild = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'MSBuild', '*' , 'bin' , 'msbuild.exe'))).Path		| Select-Object -Last 1
$VsixInstaller = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'Common7', 'IDE', 'VSIXInstaller.exe'))).Path | Select-Object -Last 1

$NugetDownloadUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$RdcManDownloadUrl = "https://download.microsoft.com/download/A/F/0/AF0071F3-B198-4A35-AA90-C68D103BDCCF/rdcman.msi"
$WixToolsetDownloadUrl = "https://wixtoolset.org/downloads/v4.0.0.5205/wix40.exe"
$WixToolsetExtensionDownloadUrl = "https://robmensching.gallerycdn.vsassets.io/extensions/robmensching/wixtoolsetvisualstudio2017extension/0.9.21.62588/1494013210879/250616/4/Votive2017.vsix"

Write-Host "========================================================"
Write-Host "=== Preparing build                                 ==="
Write-Host "========================================================"
Write-Host "Using MSBuild: $Msbuild"
Write-Host "Using VsixInstaller: $VsixInstaller"


Write-Host "========================================================"
Write-Host "=== Patch OAuth credentials                          ==="
Write-Host "========================================================"

Copy-Item -Path "${env:KOKORO_GFILE_DIR}\OAuthClient.cs" -Destination "Google.Solutions.CloudIap.IapClient\OAuthClient.cs" -Force
Copy-Item -Path "${env:KOKORO_GFILE_DIR}\OAuthClient.cs" -Destination "Google.Solutions.CloudIap.Plugin\OAuthClient.cs" -Force

Write-Host "========================================================"
Write-Host "=== Install Nuget                                    ==="
Write-Host "========================================================"

$Nuget = $env:TEMP + "\nuget.exe"
(New-Object System.Net.WebClient).DownloadFile($NugetDownloadUrl, $Nuget)


Write-Host "========================================================"
Write-Host "=== Install RDCMan                                   ==="
Write-Host "========================================================"

(New-Object System.Net.WebClient).DownloadFile($RdcManDownloadUrl, $env:TEMP + "\Rdcman.msi")

& msiexec /i $env:TEMP\Rdcman.msi /quiet /qn /norestart /log $env:TEMP\Rdcman.log | Out-Default
Get-Content -Path $env:TEMP\Rdcman.log

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Install Wix Toolset                              ==="
Write-Host "========================================================"

(New-Object System.Net.WebClient).DownloadFile($WixToolsetDownloadUrl, $env:TEMP + "\Wix.exe")
& $env:TEMP\Wix.exe -quiet -log $env:TEMP\wix.log | Out-Default
Get-Content -Path $env:TEMP\wix.log

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Install Wix Toolset Extension (Votive)           ==="
Write-Host "========================================================"

(New-Object System.Net.WebClient).DownloadFile($WixToolsetExtensionDownloadUrl, $env:TEMP + "\Votive.vsix")

& $VsixInstaller /quiet $env:TEMP\Votive.vsix | Out-Default

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Restore Nuget packages                           ==="
Write-Host "========================================================"

& $Nuget restore | Out-Default

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Build solution                                   ==="
Write-Host "========================================================"

& $Msbuild  "/t:Rebuild" "/p:Configuration=Release;Platform=x86;AssemblyVersionNumber=$ProductVersion.${env:KOKORO_BUILD_NUMBER}.0" | Out-Default

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Run tests                                        ==="
Write-Host "========================================================"

$Env:GOOGLE_APPLICATION_CREDENTIALS = "${env:KOKORO_GFILE_DIR}\iap-windows-rdc-plugin-tests.json"
$Env:GOOGLE_CLOUD_PROJECT = (Get-Content $Env:GOOGLE_APPLICATION_CREDENTIALS | Out-String | ConvertFrom-Json).project_id

$Nunit = (Resolve-Path -Path "packages\NUnit.ConsoleRunner.*\tools\nunit3-console.exe").Path
& $Nunit Google.Solutions.Compute.Test\bin\release\Google.Solutions.Compute.Test.dll `
   "--result=sponge_log.xml;transform=kokoro\nunit-to-sponge.xsl" `
   --out sponge_log.log | Out-Default

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}
