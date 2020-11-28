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

# Use TLS 1.2 for all downloads.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

${env:__BUILD_ENV_INITIALIZED} = "1"

#------------------------------------------------------------------------------
# Find MSBuild and add to PATH
#------------------------------------------------------------------------------

$Msbuild = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 
	'Microsoft Visual Studio', '*', '*', 'MSBuild', '*' , 'bin', 'msbuild.exe'))).Path | Select-Object -Last 1
if ($Msbuild)
{
	$MsbuildDir = (Split-Path $Msbuild -Parent)
	$env:Path += ";$MsbuildDir"
}
else
{
	Write-Host "Could not find msbuild" -ForegroundColor Red
	exit 1
}

#------------------------------------------------------------------------------
# Find nmake and add to PATH
#------------------------------------------------------------------------------

$Nmake = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 
	'Microsoft Visual Studio', '*', '*', 'VC', 'Tools', 'MSVC', '*' , 'bin', 'Hostx86', '*' , 'nmake.exe'))).Path | Select-Object -Last 1
if ($Nmake)
{
	$NMakeDir = (Split-Path $NMake -Parent)
	$env:Path += ";$NMakeDir"
}
else
{
	Write-Host "Could not find nmake" -ForegroundColor Red
	exit 1
}

#------------------------------------------------------------------------------
# Find nuget and add to PATH
#------------------------------------------------------------------------------

if ((Get-Command "nuget.exe" -ErrorAction SilentlyContinue) -eq $null) 
{
	$NugetDownloadUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

	New-Item -ItemType Directory -Force "${PSScriptRoot}\.tools" | Out-Null
	$Nuget = "${PSScriptRoot}\.tools\nuget.exe"
	(New-Object System.Net.WebClient).DownloadFile($NugetDownloadUrl, $Nuget)
	
	$env:Path += ";${PSScriptRoot}\.tools"
}

#------------------------------------------------------------------------------
# Restore packages and make them available in the environment
#------------------------------------------------------------------------------

if (Test-Path "*.sln")
{
	& $Nmake restore

	if ($LastExitCode -ne 0)
	{
		exit $LastExitCode
	}

	#
	# Add all tools to PATH.
	#

	$ToolsDirectories = (Get-ChildItem packages -Directory -Recurse `
		| Where-Object {$_.Name.EndsWith("tools") -or $_.FullName.Contains("tools\net4") } `
		| Select-Object -ExpandProperty FullName)

	$env:Path += ";" + ($ToolsDirectories -join ";")

	#
	# Add environment variables indicating package versions, for example
	# $env:Google_Apis_Auth = 1.2.3
	#

	(nuget list -Source (Resolve-Path packages)) `
		| ForEach-Object { New-Item -Name $_.Split(" ")[0].Replace(".", "_") -value $_.Split(" ")[1] -ItemType Variable -Path Env: }
}

#------------------------------------------------------------------------------
# Find Google Cloud credentials and project (for tests)
#------------------------------------------------------------------------------

if (!$Env:GOOGLE_APPLICATION_CREDENTIALS)
{
	$Env:GOOGLE_APPLICATION_CREDENTIALS = "${env:KOKORO_GFILE_DIR}\iap-windows-rdc-plugin-tests.json"
}

if (!${Env:GOOGLE_CLOUD_PROJECT})
{
	${Env:GOOGLE_CLOUD_PROJECT} = (Get-Content $Env:GOOGLE_APPLICATION_CREDENTIALS | Out-String | ConvertFrom-Json).project_id
}

if (!$Env:SECURECONNECT_CREDENTIALS)
{
	$Env:SECURECONNECT_CREDENTIALS = "${env:KOKORO_GFILE_DIR}\dca-user.adc.json"
}

if (!$Env:SECURECONNECT_CERTIFICATE)
{
	$Env:SECURECONNECT_CERTIFICATE = "${env:KOKORO_GFILE_DIR}\dca-user.dca.pfx"
}

Write-Host "Google Cloud project: ${Env:GOOGLE_CLOUD_PROJECT}" -ForegroundColor Yellow
Write-Host "Google Cloud credentials: ${Env:GOOGLE_APPLICATION_CREDENTIALS}" -ForegroundColor Yellow
Write-Host "SecureConnect credentials: ${Env:SECURECONNECT_CREDENTIALS}" -ForegroundColor Yellow
Write-Host "SecureConnect certificate: ${Env:SECURECONNECT_CERTIFICATE}" -ForegroundColor Yellow
Write-Host "PATH: ${Env:PATH}" -ForegroundColor Yellow

#------------------------------------------------------------------------------
# Run nmake.
#------------------------------------------------------------------------------

& $Nmake $args

if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}
