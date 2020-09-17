//
// Copyright 2020 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    public sealed class SecureConnectNativeHelper : IDisposable
    {
        //
        // The native helper is a Chrome native messaging host
        // (see https://developer.chrome.com/apps/nativeMessaging), so
        // we first need to locate its manifest.
        //
        // Because the native helper is always installed by-machine (as
        // opposed to by-user), it's sufficient to look in HKLM.
        //
        private const string HostName = "com.google.secure_connect.native_helper";
        private const RegistryHive HostRegistrationHive = RegistryHive.LocalMachine;

        private static readonly Version MinimumRequiredComponentVersion = new Version(1, 6);

        private readonly ChromeNativeMessagingHost host;


        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        private SecureConnectNativeHelper(ChromeNativeMessagingHost host)
        {
            this.host = host;
        }

        public static SecureConnectNativeHelper Start()
        {
            return new SecureConnectNativeHelper(
                ChromeNativeMessagingHost.Start(HostName, HostRegistrationHive));
        }

        public static bool IsInstalled => 
            ChromeNativeMessagingHost.FindNativeHelperLocation(
                HostName,
                HostRegistrationHive) != null;

        public void Dispose()
        {
            this.host.Dispose();
        }

        //---------------------------------------------------------------------
        // Messages.
        //---------------------------------------------------------------------

        public void Ping()
        {
            var request = new PingRequest();
            var response = this.host.TransactMessage<PingRequest, PingResponse>(request);

            Debug.Assert(response.CommandId == request.CommandId);

            if (response.Details.ComponentVersion < MinimumRequiredComponentVersion)
            {
                throw new SecureConnectException(
                    $"Installed version {response.Details.ComponentVersion} is older " +
                    $"than required version {MinimumRequiredComponentVersion}");
            }
        }

        public bool? ShouldEnrollDevice(string userId)
        {
            var request = new ShouldEnrollDeviceRequest(userId);
            var response = this.host.TransactMessage<
                ShouldEnrollDeviceRequest, ShouldEnrollDeviceResponse>(request);

            Debug.Assert(response.CommandId == request.CommandId);

            return response.ShouldEnrollDevice;
        }

        //---------------------------------------------------------------------
        // Messages.
        //---------------------------------------------------------------------

        public abstract class RequestBase
        {
            [JsonProperty("protocolVersion")]
            public string ProtocolVersion { get; } = "0.2";

            [JsonProperty("commandID")]
            public int CommandId { get; }

            [JsonProperty("commandType")]
            public string CommandType { get; }

            public RequestBase(int commandId, string commandType)
            {
                this.CommandId = commandId;
                this.CommandType = commandType;
            }
        }

        public abstract class ResponseBase
        {
            [JsonProperty("commandID")]
            public int CommandId { get; set; }
        }

        //---------------------------------------------------------------------
        // Ping.
        //---------------------------------------------------------------------

        public class PingRequest : RequestBase
        {
            public PingRequest() : base(18, "ping")
            {
            }
        }

        public class PingResponse : ResponseBase
        {
            public class Payload
            {
                [JsonProperty("componentVersion")]
                public Version ComponentVersion { get; set; }
            }

            [JsonProperty("ping")]
            public Payload Details { get; set; }
        }

        //---------------------------------------------------------------------
        // ShouldEnrollDevice.
        //---------------------------------------------------------------------

        public class ShouldEnrollDeviceRequest : RequestBase
        {
            public class Payload
            {
                [JsonProperty("userId")]
                public string UserId { get; }

                public Payload(string userId)
                {
                    this.UserId = userId;
                }
            }

            [JsonProperty("arguments")]
            public Payload Arguments { get; set; }

            public ShouldEnrollDeviceRequest(string userId) : base(19, "shouldEnrollDevice")
            {
                this.Arguments = new Payload(userId);
            }
        }

        public class ShouldEnrollDeviceResponse : ResponseBase
        {
            [JsonProperty("shouldEnrollDevice")]
            public bool? ShouldEnrollDevice { get; set; }
        }

    }

    public class SecureConnectException : Exception
    {
        public SecureConnectException(string message)
            : base(message)
        {
        }

        public SecureConnectException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
