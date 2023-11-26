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

#Requires -RunAsAdministrator
$ErrorActionPreference = "stop"

$TraceName = "IapDesktop"
$Providers = @{
	#
	# Internal providers.
	#
	"Google-Solutions-Apis" = "{EC3585B8-5C28-42AE-8CE7-D76CB00303C6}"
	"Google-IapDesktop-Application" = "{4B23296B-C25A-449C-91F2-897BDABAA1A8}"
	"Google-Solutions-Ssh" = "{7FCCFB8B-ABEC-4ADB-B994-E631DD56AA8C}"

	#
	# Relevant system providers.
	#
	"Microsoft-Windows-TerminalServices-ClientActiveXCore" = "{28AA95BB-D444-4719-A36F-40462168127E}"
}

#
# Create session and enable all providers.
#
$Timestamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$Filename = "IapDesktop_$($Timestamp).etl"

& logman create trace $TraceName -ets -o $Filename | Out-Default
$Providers.Values | % { &logman update trace -ets $TraceName -p $_ | Out-Default }

#
# Run session until the user stops it.
#
& logman query $TraceName -ets | Out-Default

Write-Host "Trace session started ($Filename). Press enter to stop." -ForegroundColor Yellow
Read-Host

& logman stop   $TraceName -ets | Out-Default
Write-Host "Trace session stopped ($Filename)." -ForegroundColor Yellow
