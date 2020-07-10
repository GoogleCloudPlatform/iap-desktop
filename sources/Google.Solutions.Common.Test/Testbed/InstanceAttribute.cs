//
// Copyright 2019 Google LLC
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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Google.Solutions.Common.Test.Testbed
{
    public abstract class InstanceAttribute : NUnitAttribute, IParameterDataSource
    {
        public string ProjectId { get; set; } = Defaults.ProjectId;
        public string Zone { get; set; } = Defaults.Zone;
        public string MachineType { get; set; } = "n1-standard-1";
        public string ImageFamily { get; set; }
        public string InitializeScript { get; set; }

        protected abstract string InstanceNamePrefix { get; }
        protected abstract IEnumerable<Metadata.ItemsData> Metadata { get; }

        private string CreateSpecificationFingerprint()
        {
            // Create a hash of the image specification.
            var imageSpecification = new StringBuilder();
            imageSpecification.Append(this.MachineType);
            imageSpecification.Append(this.ImageFamily);
            imageSpecification.Append(this.InitializeScript);

            var kokoroJobType = Environment.GetEnvironmentVariable("KOKORO_JOB_TYPE");
            if (!string.IsNullOrEmpty(kokoroJobType))
            {
                // Prevent different job types sharing the same VMs.
                imageSpecification.Append(kokoroJobType);
            }

            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                var imageSpecificationRaw = Encoding.UTF8.GetBytes(imageSpecification.ToString());
                return this.InstanceNamePrefix + BitConverter
                    .ToString(sha.ComputeHash(imageSpecificationRaw))
                    .Replace("-", String.Empty)
                    .Substring(0, 14)
                    .ToLower();
            }
        }

        public override string ToString()
        {
            return this.CreateSpecificationFingerprint();
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(InstanceRequest))
            {
                var vmRef = new InstanceLocator(
                    this.ProjectId,
                    this.Zone,
                    this.CreateSpecificationFingerprint());
                yield return new InstanceRequest(
                    vmRef, 
                    this.MachineType,
                    this.ImageFamily,
                    this.Metadata);
            }
            else
            {
                throw new ArgumentException(
                    $"Parameter must be of type {typeof(InstanceLocator).Name}");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class WindowsInstanceAttribute : InstanceAttribute
    {
        public const string DefaultMachineType = "n1-standard-2";

        public const string WindowsServer2019 = "projects/windows-cloud/global/images/family/windows-2019";
        public const string WindowsServerCore2019 = "projects/windows-cloud/global/images/family/windows-2019-core";

        protected override string InstanceNamePrefix => "w";

        public WindowsInstanceAttribute()
        {
            this.MachineType = DefaultMachineType;
            this.ImageFamily = WindowsServerCore2019;
        }

        protected override IEnumerable<Metadata.ItemsData> Metadata
        {
            get
            {
                yield return new Metadata.ItemsData()
                {
                    Key = "sysprep-specialize-script-ps1",
                    Value = this.InitializeScript
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "enable-guest-attributes",
                    Value = "TRUE"
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "windows-startup-script-ps1",
                    Value = "Invoke-RestMethod " +
                        "-Headers @{\"Metadata-Flavor\"=\"Google\"} " +
                        "-Method PUT " +
                        "-Uri http://metadata.google.internal/computeMetadata/v1/instance/" +
                        $"guest-attributes/{InstanceRequest.GuestAttributeNamespace}/{InstanceRequest.GuestAttributeKey} " +
                        "-Body TRUE"
                };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class LinuxInstanceAttribute : InstanceAttribute
    {
        public const string DefaultMachineType = "f1-micro";
        public const string Debian9 = "projects/debian-cloud/global/images/family/debian-9";

        protected override string InstanceNamePrefix => "u";

        public LinuxInstanceAttribute()
        {
            this.MachineType = DefaultMachineType;
            this.ImageFamily = Debian9;
        }

        protected override IEnumerable<Metadata.ItemsData> Metadata
        {
            get
            {
                var script = this.InitializeScript != null
                    ? this.InitializeScript + ";"
                    : string.Empty;

                yield return new Metadata.ItemsData()
                {
                    Key = "enable-guest-attributes",
                    Value = "TRUE"
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "startup-script",
                    Value = script +
                        "curl -X PUT --data \"TRUE\" " +
                        "http://metadata.google.internal/computeMetadata/v1/instance/" +
                        $"guest-attributes/{InstanceRequest.GuestAttributeNamespace}/{InstanceRequest.GuestAttributeKey} " +
                        "-H \"Metadata-Flavor: Google\""
                };
            }
        }
    }
}
