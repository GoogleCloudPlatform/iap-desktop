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
$ProductVersion="2.1"

$Msbuild = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'MSBuild', '*' , 'bin' , 'msbuild.exe'))).Path		| Select-Object -Last 1
$VsixInstaller = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'Common7', 'IDE', 'VSIXInstaller.exe'))).Path | Select-Object -Last 1

$NugetDownloadUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$RdcManDownloadUrl = "https://download.microsoft.com/download/A/F/0/AF0071F3-B198-4A35-AA90-C68D103BDCCF/rdcman.msi"

Write-Host "========================================================"
Write-Host "=== Preparing build                                 ==="
Write-Host "========================================================"
Write-Host "Using MSBuild: $Msbuild"
Write-Host "Using VsixInstaller: $VsixInstaller"


Write-Host "========================================================"
Write-Host "=== Checking copyright headers                       ==="
Write-Host "========================================================"

$FilesWithoutCopyrightHeader = (Get-ChildItem -Recurse `
    | Where-Object {$_.Name.EndsWith(".cs")} `
    | Where-Object {$_.Name -ne "OAuthClient.cs"} `
    | Where-Object {$_.Name -ne "Settings.Designer.cs"} `
    | Where-Object {$_.Name -ne "Resources.Designer.cs"} `
    | Where-Object {-not $_.Name.StartsWith("TemporaryGeneratedFile")} `
    | Where-Object {-not [System.IO.File]::ReadAllText($_.FullName).Contains("Copyright")}  `
    | Select-Object -ExpandProperty FullName)
    
if ($FilesWithoutCopyrightHeader)
{
    Write-Host("Multiple files lack a copyright header")
    $FilesWithoutCopyrightHeader
    exit 1
}

Write-Host "========================================================"
Write-Host "=== Patch OAuth credentials                          ==="
Write-Host "========================================================"

Copy-Item -Path "${env:KOKORO_GFILE_DIR}\OAuthClient.cs" -Destination "Google.Solutions.IapDesktop\OAuthClient.cs" -Force

Write-Host "========================================================"
Write-Host "=== Install Nuget                                    ==="
Write-Host "========================================================"

$Nuget = $env:TEMP + "\nuget.exe"
(New-Object System.Net.WebClient).DownloadFile($NugetDownloadUrl, $Nuget)


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
Write-Host "=== Build installer                                  ==="
Write-Host "========================================================"

.\build-installer.ps1 -ProductVersion "$ProductVersion.${env:KOKORO_BUILD_NUMBER}.0" -Configuration Release

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Run tests                                        ==="
Write-Host "========================================================"

$Env:GOOGLE_APPLICATION_CREDENTIALS = "${env:KOKORO_GFILE_DIR}\iap-windows-rdc-plugin-tests.json"
$Env:GOOGLE_CLOUD_PROJECT = (Get-Content $Env:GOOGLE_APPLICATION_CREDENTIALS | Out-String | ConvertFrom-Json).project_id

# NB. The OpenCover version must match the CLR version installed on Kokoro. The version
# is defined in the NuGet dependencies of the main project.

$OpenCover = (Resolve-Path -Path "packages\OpenCover.*\tools\OpenCover.Console.exe").Path
$Nunit = (Resolve-Path -Path "packages\NUnit.ConsoleRunner.*\tools\nunit3-console.exe").Path

$NunitArguments = `
    "Google.Solutions.Compute.Test\bin\release\Google.Solutions.Compute.Test.dll " + `
    "Google.Solutions.Audit.Test\bin\release\Google.Solutions.Audit.Test.dll " + `
    "Google.Solutions.IapDesktop.Application.Test\bin\release\Google.Solutions.IapDesktop.Application.Test.dll " + `
    "--result=sponge_log.xml;transform=kokoro\nunit-to-sponge.xsl "
#    "--where \""cat != IntegrationTest\"""

& $OpenCover `
    -register:user `
    -returntargetcode `
    -target:$Nunit `
    "-targetargs:$NunitArguments" `
    -filter:"+[Google.Solutions.Compute]* +[Google.Solutions.Audit]* +[Google.Solutions.IapDesktop.Application]*" `
    -output:opencovertests.xml | Out-Default

if ($LastExitCode -ne 0)
{
    Write-Host "Tests failed: $LastExitCode"
    exit $LastExitCode
}


Write-Host "========================================================"
Write-Host "=== Create code coverage report                      ==="
Write-Host "========================================================"

$ReportGenerator = (Resolve-Path -Path "packages\ReportGenerator.*\tools\net4*\ReportGenerator.exe").Path

&$ReportGenerator `
    "-reports:opencovertests.xml" `
    "-targetdir:coveragereport" `
    -reporttypes:HTML

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}
