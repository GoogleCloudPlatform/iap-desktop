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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Google.Solutions.Common.Util;
using System.Linq;
using Google.Solutions.Common.Diagnostics;

namespace Google.Solutions.IapDesktop.Application.Services.SecureConnect
{
    public interface ISecureConnectAdapter
    {
        bool IsInstalled { get; }
        bool IsDeviceEnrolledForUser(string userId);
        ISecureConnectDeviceInfo DeviceInfo { get; }
    }

    public interface ISecureConnectDeviceInfo
    {
        string SerialNumber { get; }
        IEnumerable<string> CertificateThumbprints { get; }
    }

    public sealed class SecureConnectAdapter : ISecureConnectAdapter
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

        // The command ID is for debugging only, the native helper does not interpret it.
        private int commandId = 1;

        private static ChromeNativeMessagingHost StartHost()
        {
            // 
            // NB. An instance is only suitable to dispatch a single command, 
            // it auto-terminated after that.
            //
            return ChromeNativeMessagingHost.Start(HostName, HostRegistrationHive);
        }

        //---------------------------------------------------------------------
        // Messages.
        //---------------------------------------------------------------------

        private TResponse TransactMessage<TRequest, TResponse>(
            ChromeNativeMessagingHost host,
            TRequest request) where TResponse : ResponseBase
        {
            var response = JsonConvert.DeserializeObject<TResponse>(
                host.TransactMessage(JsonConvert.SerializeObject(request)));
            response.ThrowIfFailed();
            return response;
        }

        //---------------------------------------------------------------------
        // ISecureConnectAdapter.
        //---------------------------------------------------------------------

        public bool IsInstalled
        {
            get
            {
                if (ChromeNativeMessagingHost.FindNativeHelperLocation(
                    HostName,
                    HostRegistrationHive) == null)
                {
                    // Extension not found.
                    TraceSources.IapDesktop.TraceWarning(
                        "SecureConnect native messaging host not found");

                    return false;
                }

                //
                // Validate installed version.
                //

                using (var host = StartHost())
                {
                    var request = new PingRequest(Interlocked.Increment(ref this.commandId));
                    var response = TransactMessage<PingRequest, PingResponse>(host, request);

                    Debug.Assert(response.CommandId == request.CommandId);

                    if (response.Ping.ComponentVersion < MinimumRequiredComponentVersion)
                    {
                        TraceSources.IapDesktop.TraceWarning(
                            "Installed version {0} is older than required version {1}",
                            response.Ping.ComponentVersion,
                            MinimumRequiredComponentVersion);
                        return false;
                    }
                }

                return true;
            }
        }
            

        public bool IsDeviceEnrolledForUser(string userId)
        {
            using (var host = StartHost())
            {
                var request = new ShouldEnrollDeviceRequest(
                    Interlocked.Increment(ref this.commandId),
                    userId);
                var response = TransactMessage<ShouldEnrollDeviceRequest, ShouldEnrollDeviceResponse>(
                    host,
                    request);

                Debug.Assert(response.CommandId == request.CommandId);

                return response.ShouldEnrollDevice == false;
            }
        }

        public ISecureConnectDeviceInfo DeviceInfo
        {
            get
            {
                using (var host = StartHost())
                {
                    var request = new DeviceInfoRequest(
                        Interlocked.Increment(ref this.commandId),
                        new[] { "certificates", "serial_number", "model" });
                    var response = TransactMessage<DeviceInfoRequest, DeviceInfoResponse>(
                        host,
                        request);

                    Debug.Assert(response.CommandId == request.CommandId);

                    return new SecureConnectDeviceInfo(
                        response.DeviceInfo.SerialNumber,
                        response.DeviceInfo.Certificates
                            .EnsureNotNull()
                            .Select(cert => cert.Fingerprint));
                }
            }
        }

        //---------------------------------------------------------------------
        // Message: base classes.
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


            [JsonProperty("error")]
            public string Error { get; set; }

            public void ThrowIfFailed()
            {
                if (!string.IsNullOrEmpty(this.Error))
                {
                    throw new SecureConnectException(this.Error);
                }
            }
        }

        //---------------------------------------------------------------------
        // Message: Ping.
        //---------------------------------------------------------------------

        public class PingRequest : RequestBase
        {
            public PingRequest(int commandId) : base(commandId, "ping")
            {
            }
        }

        public class PingResponse : ResponseBase
        {
            public class PingDetails
            {
                [JsonProperty("componentVersion")]
                public Version ComponentVersion { get; set; }
            }

            [JsonProperty("ping")]
            public PingDetails Ping { get; set; }
        }

        //---------------------------------------------------------------------
        // Message: ShouldEnrollDevice.
        //---------------------------------------------------------------------

        public class ShouldEnrollDeviceRequest : RequestBase
        {
            public class ArgumentDetails
            {
                [JsonProperty("userId")]
                public string UserId { get; }

                public ArgumentDetails(string userId)
                {
                    this.UserId = userId;
                }
            }

            [JsonProperty("arguments")]
            public ArgumentDetails Arguments { get; set; }

            public ShouldEnrollDeviceRequest(int commandId, string userId) 
                : base(commandId, "shouldEnrollDevice")
            {
                this.Arguments = new ArgumentDetails(userId);
            }
        }

        public class ShouldEnrollDeviceResponse : ResponseBase
        {
            [JsonProperty("shouldEnrollDevice")]
            public bool? ShouldEnrollDevice { get; set; }
        }

        //---------------------------------------------------------------------
        // Message: DeviceInfo.
        //---------------------------------------------------------------------

        public class DeviceInfoRequest : RequestBase
        {
            public class ArgumentDetails
            {
                [JsonProperty("attributes")]
                public IList<string> Attributes { get; set; }

                public ArgumentDetails(IList<string> attributes)
                {
                    this.Attributes = attributes;
                }
            }

            [JsonProperty("arguments")]
            public ArgumentDetails Arguments { get; set; }

            public DeviceInfoRequest(
                int commandId,
                IList<string> attributes
                ) : base(commandId, "deviceInfo")
            {
                this.Arguments = new ArgumentDetails(attributes);
            }
        }

        public class DeviceInfoResponse : ResponseBase
        {
            public class DeviceCertificate
            {
                [JsonProperty("fingerprint")]
                public string Fingerprint { get; set; }

                [JsonProperty("origin")]
                public int Origin { get; set; }
            }

            public class DeviceInfoDetails
            {
                [JsonProperty("certificates")]
                public IList<DeviceCertificate> Certificates { get; set; }

                [JsonProperty("serialNumber")]
                public string SerialNumber { get; set; }
            }

            [JsonProperty("deviceInfo")]
            public DeviceInfoDetails DeviceInfo { get; set; }
        }
    }

    public class SecureConnectDeviceInfo : ISecureConnectDeviceInfo
    {
        [JsonProperty("certificates")]
        public IEnumerable<string> CertificateThumbprints { get; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; }

        public SecureConnectDeviceInfo(
            string serialNumber,
            IEnumerable<string> certificateThumbprints)
        {
            this.SerialNumber = serialNumber;
            this.CertificateThumbprints = certificateThumbprints;
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
