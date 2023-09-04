#
# Copyright 2013 Google LLC
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
	"Google-Solutions-Api" = "{EC3585B8-5C28-42AE-8CE7-D76CB00303C6}"
	"Google-IapDesktop-Application" = "{4B23296B-C25A-449C-91F2-897BDABAA1A8}"
}

#
# Create session and enable all providers.
#
& logman create trace $TraceName -ets -o IapDesktop.etl | Out-Default
$Providers.Values | % { &logman update trace -ets $TraceName -p $_ | Out-Default }

#
# Run session until the user stops it.
#
& logman query $TraceName -ets | Out-Default

Write-Host "Trace session started. Press enter to stop." -ForegroundColor Yellow
Read-Host

& logman stop   $TraceName -ets | Out-Default
Write-Host "Trace session stopped." -ForegroundColor Yellow
