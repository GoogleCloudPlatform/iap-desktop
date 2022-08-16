#
# Copyright 2022 Google LLC
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

$ErrorActionPreference = "Stop"

Import-Module "${Env:ProgramFiles}\Google\Compute Engine\sysprep\gce_base.psm1"

$OperationId = "{{OPERATION_ID}}"
$MetadataKey = "iapdesktop-join";

#
# Create a new, ephemeral RSA key.
#
$Key = New-Object -TypeName System.Security.Cryptography.RSACng -ArgumentList 2048
$KeyParameters = $Key.ExportParameters($false)

#
# Write "hello" message to serial port. This message
# contains the public key portion of the ephemeral RSA key.
# The client uses that public key to encrypt credentials.
#

Write-SerialPort -portname COM4 -Data (@{
    "OperationId" = $OperationId
    "MessageType" = "hello"
    "Modulus" = [Convert]::ToBase64String($KeyParameters.Modulus)
    "Exponent" = [Convert]::ToBase64String($KeyParameters.Exponent)
} | ConvertTo-Json -Compress)

#
# Wait for "join-request" message in metadata.
#
Write-Host "Waiting for request to initiate domain join..."

$TotalWaitMillis = 20000
do {
    $WaitMillis = 500
    $TotalWaitMillis = $TotalWaitMillis - $WaitMillis

    Start-Sleep -Milliseconds $WaitMillis
    $MetadataValue = Get-MetaData -Property "attributes/$MetadataKey"
} while (-not $MetadataValue -and $TotalWaitMillis -gt 0)

if (-not $MetadataValue) {
    throw [System.TimeoutException]::new(
        "Timeout elapsed waiting for 'join-request' message from client")
}

try {
    $JoinRequest = $MetadataValue | ConvertFrom-Json

    if (($JoinRequest.OperationId -ne $OperationId) -or 
        
    ($JoinRequest.MessageType -ne "join-request")) {
        throw [System.ArgumentException]::new(
            "Encountered unexpected request message $($JoinRequest.MessageType) in metadata")
    }

    #
    # Decrypt password using our ephemeral key.
    #
    $PlainTextPassword = [System.Text.Encoding]::UTF8.GetString(
        $Key.Decrypt(
            [Convert]::FromBase64String($JoinRequest.EncryptedPassword), 
            [System.Security.Cryptography.RSAEncryptionPadding]::Pkcs1))

    $Password = ConvertTo-SecureString `
        -AsPlainText `
        -Force `
        -String $PlainTextPassword

    #
    # Join and restart.
    #
    Write-Host "Joining computer as $($JoinRequest.NewComputerName) to domain $($JoinRequest.DomainName)..."
    if ($JoinRequest.NewComputerName -ne $null) {
        Add-Computer `
            -ComputerName localhost `
            -DomainName $JoinRequest.DomainName `
            -NewName  $JoinRequest.NewComputerName `
            -Credential (New-Object PSCredential ($JoinRequest.Username, $Password)) `
            -Force
    }
    else {
        Add-Computer `
            -ComputerName localhost `
            -DomainName $JoinRequest.DomainName `
            -Credential (New-Object PSCredential ($JoinRequest.Username, $Password)) `
            -Force
    }

    #
    # Join succeeded, write response message.
    #
    Write-SerialPort -portname COM4 -Data (@{
            "OperationId" = $OperationId
            "MessageType" = "join-response"
            "Succeeded" = $True
        } | ConvertTo-Json -Compress)

    Write-Host "Domain join completed, restarting..."
    Restart-Computer
}
catch {
    #
    # Join failed, write response message.
    #
    Write-Host "Domain join failed: $($_.Exception.Message)"
    Write-SerialPort -portname COM4 -Data (@{
            "OperationId" = $OperationId
            "MessageType" = "join-response"
            "Succeeded" = $False
            "ErrorDetails" = $($_.Exception.Message)
        } | ConvertTo-Json -Compress)
}