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

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$True)]$ProductVersion,
    [Parameter(Mandatory=$True)]$Configuration
)

$ErrorActionPreference = "stop"

$WixTools = (Resolve-Path ([IO.Path]::Combine('packages', 'WiX.*', 'tools'))).Path
$Candle = Join-Path $WixTools 'candle.exe'
$Light = Join-Path $WixTools 'light.exe'

$SourcesDir = "${PSScriptRoot}\..\installer"
$ObjDir = "${PSScriptRoot}\..\installer\bin"

#
# Compile MSI.
#

& $Candle `
    -nologo `
    -out "$ObjDir\$Configuration\" `
    "-dCONFIGURATION=$Configuration" `
    "-dVERSION=$ProductVersion" `
    "-dBASEDIR=$SourcesDir" `
    -arch x86 `
    -ext "$WixTools\WixUIExtension.dll" `
    -ext "$WixTools\WixUtilExtension.dll" `
    "$SourcesDir\Product.wxs"

#
# Link MSI.
#

& $Light `
    -nologo `
    -out "$ObjDir\$Configuration\IapDesktop-$ProductVersion.msi" `
    -sw1076 `
    -cultures:null `
    -ext "$WixTools\WixUIExtension.dll" `
    -ext "$WixTools\WixUtilExtension.dll" `
    "$ObjDir\$Configuration\Product.wixobj"

#
# Package symbols.
#

$SymbolsDir = Join-Path -Path (Resolve-Path -Path "$ObjDir\$Configuration") -ChildPath "Symbols"
$SymbolsArchive = Join-Path -Path (Resolve-Path -Path "$ObjDir\$Configuration") -ChildPath "Symbols-$ProductVersion.zip"

if (Test-path $SymbolsArchive) {
    Remove-item $SymbolsArchive
}

New-Item -Type Directory -Force $SymbolsDir | Out-Null
Copy-Item -Path "Google.Solutions.IapDesktop\bin\$Configuration\*.pdb" -Destination $SymbolsDir

Add-Type -Assembly "System.IO.Compression.FileSystem"
[System.IO.Compression.ZipFile]::CreateFromDirectory($SymbolsDir, $SymbolsArchive)