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

# Http Server
$Listener = [System.Net.HttpListener]::new() 
$Listener.Prefixes.Add("http://localhost:8080/")
$Listener.Start()

if ($Listener.IsListening) {
    Write-Host " HTTP Server Ready! " -f 'black' -b 'gre'
}

while ($Listener.IsListening) {
    $Context = $Listener.GetContext()

    Write-Host "$($Context.Request.UserHostAddress)  =>  $($context.Request.Url)" -f 'mag'

    [string]$Response = @{
        "User" = $Context.Request.QueryString["User"]
        "Domain" = $Context.Request.QueryString["Domain"]
        "Password" = $Context.Request.QueryString["Password"]
    } | ConvertTo-Json

    $Buffer = [System.Text.Encoding]::UTF8.GetBytes($Response)
    $Context.Response.ContentLength64 = $Buffer.Length
    $Context.Response.OutputStream.Write($Buffer, 0, $Buffer.Length)
    $Context.Response.OutputStream.Close()
} 