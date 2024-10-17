#
# Copyright 2024 Google LLC
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

#
# Create WinGet manifests for a GitHub release.
#

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$False)][string]$ReleaseUrl = "https://api.github.com/repos/GoogleCloudPlatform/iap-desktop/releases/latest",

    [Parameter(Mandatory=$False)]
    [ValidateSet('Version','DefaultLocale','Installer')]
    [string]$ManifestType = "Version"
)

$ErrorActionPreference = "stop"
$GithubRepository = "GoogleCloudPlatform/iap-desktop"

#
# Return all entries of an MSI's Property table.
#
function Get-MsiProperties {
    Param(
        [Parameter(Mandatory=$True)][string]$Package
    )

    $Installer = New-Object -ComObject WindowsInstaller.Installer
    try {
        $PackageDatabase = $Installer.OpenDatabase($Package, 0)
        $View = $PackageDatabase.OpenView('SELECT `Property`, `Value` FROM `Property`')
        $View.Execute()
        $Record = $view.Fetch()

        $Properties = @{}
        while ($Record -ne $null) {
            $Properties.Add($Record.StringData(1), $Record.StringData(2))
            $Record = $View.Fetch()
        }

        $View.Close()
    
        return $Properties
    }
    catch {
        throw "The file does not exist or is not a valid MSI database: " + $_.Exception;
    }
}

#
# Get architecture of an MSI package, which is encoded in
# the Template summary property, see
# https://learn.microsoft.com/en-au/windows/win32/msi/template-summary
#
function Get-MsiArchitecture {
    Param(
        [Parameter(Mandatory=$True)][string]$Package
    )

    # Map to architecture IDs as use bt WinGet.
    $Architectures = @{
        "Intel" = "x86"
        "x64" = "x64"
        "arm64" = "arm64"
    }

    # Open MSI database to query metadata.
    $Installer = New-Object -ComObject WindowsInstaller.Installer
    try {
        $SummaryInfo = $Installer.SummaryInformation($Package)

        $Architecture = ($SummaryInfo.Property(7) -split ";")[0]
        return $Architectures[$Architecture]
    }
    catch {
        throw "The file does not exist or is not a valid MSI database: " + $_.Exception;
    }
}

#
# Get release metadata from GitHub.
# 
$Release = Invoke-RestMethod -Uri $ReleaseUrl
if (-not $Release.id) {
    throw "The URL does not refer to a GitHub release"
}


#
# Extract relevant assets.
#
$MsiAssets = $Release.assets.Where{$_.browser_download_url -match 'iapdesktop(.+)\.msi'}
if (!$MsiAssets) {
    throw "The release does not contain any matching MSI packages"
}

#
# Download MSI files to temp folder.
#
$MsiFiles = @{}
foreach ($MsiAsset in $MsiAssets) {
    $LocalFile = "$($env:Temp)\$($MsiAsset.id)_$($MsiAsset.name)"
    if (!(Test-Path $LocalFile)) {
        Write-Host "Downloading $($MsiAsset.name) to $LocalFile..." -ForegroundColor Yellow
    
        Start-BitsTransfer `
            -Source $MsiAsset.browser_download_url `
            -Destination $LocalFile

    }
        
    $MsiFiles[$MsiAsset.id] = $LocalFile
}

#
# Generate the requested type of manifest.
#
$SharedProperties = Get-MsiProperties $MsiFiles[$MsiAssets[0].id]

if ($ManifestType -eq "Version") {
    #
    # Version manifest.
    #

    $Manifest = @"
ManifestType: "version"
ManifestVersion: 1.9.0
PackageIdentifier: Google.IAPDesktop
PackageVersion: '$($SharedProperties.ProductVersion)'

DefaultLocale: "en-US"
"@
}

elseif ($ManifestType -eq "DefaultLocale") {
    #
    # Manifest for default locale.
    #

    $Manifest = @"
ManifestType: "defaultLocale"
ManifestVersion: 1.9.0
PackageIdentifier: Google.IAPDesktop
PackageVersion: '$($SharedProperties.ProductVersion)'
PackageLocale: en-US

PackageName: IAP Desktop
Author: GoogleCloudPlatform
Publisher: Google LLC
PublisherUrl: https://github.com/$GithubRepository
PublisherSupportUrl: https://github.com/$GithubRepository/issues

License: Apache License 2.0
LicenseUrl: https://raw.githubusercontent.com/$GithubRepository/master/LICENSE.txt

Copyright: Copyright 2024 Google, Inc.
CopyrightUrl: https://raw.githubusercontent.com/$GithubRepository/master/LICENSE.txt
ShortDescription: IAP Desktop is an open-source Remote Desktop and SSH client that lets you connect to your Google Cloud VM instances from anywhere.
Moniker: iap-desktop
Tags:
- google-cloud
- iap
- ssh
- rdp
- windows
ReleaseNotesUrl: https://github.com/$GithubRepository/releases/tag/$($Release.tag_name)
"@
}

elseif ($ManifestType -eq "Installer") {
    #
    # Installer manifest.
    #

$Manifest = @"
ManifestType: installer
ManifestVersion: 1.9.0
PackageIdentifier: Google.IAPDesktop
PackageVersion: '$($SharedProperties.ProductVersion)'

Platform:
- Windows.Desktop
InstallerLocale: en-US
InstallerType: wix
Scope: user
InstallModes:
- interactive
- silent
- silentWithProgress
UpgradeBehavior: install
ReleaseDate: $($Release.published_at)
AppsAndFeaturesEntries:
- UpgradeCode: '$($SharedProperties.UpgradeCode)'
Installers:
"@

    foreach ($MsiAsset in $MsiAssets) {
    $MsiFile = $MsiFiles[$MsiAsset.id]

    $Architecture = Get-MsiArchitecture $MsiFile
    $Properties = Get-MsiProperties $MsiFile
    $Hash = Get-FileHash -Algorithm SHA256 $MsiFile

    $Manifest += @"

- Architecture: $($Architecture)
  ProductCode: '$($Properties.ProductCode)'
  InstallerUrl: $($MsiAsset.browser_download_url)
  InstallerSha256: $($Hash.Hash)
"@
    }
}

else {
    throw "The manifest type is not supported"
}

#
# Write to stdout.
#
$Manifest