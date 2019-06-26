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

$Msbuild = (Resolve-Path ([IO.Path]::Combine(${Env:ProgramFiles(x86)}, 'Microsoft Visual Studio', '*', '*', 'MSBuild', '*' , 'bin' , 'msbuild.exe'))).Path
$Nuget = "c:\nuget\nuget.exe"

Write-Host "=== Restore Nuget packages ==="
& $Nuget restore | Out-Default
if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}

Write-Host "=== Build solution ==="
#& $Msbuild  "/t:Plugin_Google_CloudIap:Rebuild" "/p:Configuration=Release;Platform=x86" | Out-Default
if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}