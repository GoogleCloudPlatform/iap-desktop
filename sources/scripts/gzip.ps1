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

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$True)]$File
)

$ErrorActionPreference = "stop"

$InputFile = Get-Item -Path $File
$OutFile = "$($InputFile.FullName).gz"

try
{
    $Input = New-Object System.IO.FileStream($InputFile.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $Output = New-Object System.IO.FileStream($OutFile, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $Zip = New-Object System.IO.Compression.GZipStream($Output, [System.IO.Compression.CompressionMode]::Compress)
    $Input.CopyTo($Zip)
} 
catch
{
    Write-Host "$_.Exception.Message" -ForegroundColor Red
    throw
}
finally
{
    $Zip.Dispose()
    $Input.Dispose()
    $Output.Dispose()
}