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

$MsbuildDir = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'MSBuild'))).Path
$Msbuild = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'MSBuild', '*' , 'bin' , 'msbuild.exe'))).Path
$Nuget = "c:\nuget\nuget.exe"
$RdcManDownloadUrl = "https://download.microsoft.com/download/A/F/0/AF0071F3-B198-4A35-AA90-C68D103BDCCF/rdcman.msi"
$WixToolsetDownloadUrl = "https://wixtoolset.org/downloads/v4.0.0.5205/wix40.exe"
$WixDefaultInstallDir = "${Env:ProgramFiles(x86)}\WiX Toolset v4.0"

Write-Host "========================================================"
Write-Host "=== Preparing build                                 ==="
Write-Host "========================================================"
Write-Host "Using MSBuild: $Msbuild"

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

# Install targets so that msbuild can find them.
New-Item -path "$MsbuildDir\WiX Toolset\v4" -type directory
Copy-Item -Path "$WixDefaultInstallDir\bin\*.targets" -Destination "$MsbuildDir\WiX Toolset\v4"


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

& $Msbuild  "/t:Rebuild" "/p:Configuration=Release;Platform=x86" | Out-Default

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}