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
using Google.Solutions.Apis.Locator;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Google.Solutions.Testing.Apis.Integration
{
    public abstract class InstanceAttribute : NUnitAttribute, IParameterDataSource
    {
        public string ProjectId { get; set; } = TestProject.ProjectId;
        public string Zone { get; set; } = TestProject.Zone;
        public string MachineType { get; set; } = "n1-standard-1";
        public abstract string ImageFamily { get; set; }
        public bool PublicIp { get; set; } = true;
        public string? InitializeScript { get; set; }
        public InstanceServiceAccount ServiceAccount { get; set; } = InstanceServiceAccount.None;

        public bool EnableOsInventory { get; set; } = false;
        public bool EnableOsLogin { get; set; } = false;

        protected abstract string InstanceNamePrefix { get; }
        protected abstract IEnumerable<Metadata.ItemsData> Metadata { get; }

        protected static string CreateUniqueGuestAttributeKey()
        {
            return "completed-" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        private string CreateSpecificationFingerprint()
        {
            //
            // Create a hash of the VM specification.
            //
            var specification = new StringBuilder()
                .Append(TestProject.ProjectId)
                .Append(this.MachineType)
                .Append(this.ImageFamily)
                .Append(this.PublicIp)
                .Append(this.InitializeScript)
                .Append(this.EnableOsInventory)
                .Append(this.EnableOsLogin)
                .Append(this.ServiceAccount.ToString())
                .Append(GetType().Name);

            var kokoroJobType = Environment.GetEnvironmentVariable("KOKORO_JOB_TYPE");
            if (!string.IsNullOrEmpty(kokoroJobType))
            {
                // Prevent different job types sharing the same VMs.
                specification.Append(kokoroJobType);
            }

            using (var sha = SHA256.Create())
            {
                var specificationRaw = Encoding.UTF8.GetBytes(specification.ToString());
                return this.InstanceNamePrefix + BitConverter
                    .ToString(sha.ComputeHash(specificationRaw))
                    .Replace("-", string.Empty)
                    .Substring(0, 14)
                    .ToLower();
            }
        }

        public override string ToString()
        {
            return CreateSpecificationFingerprint();
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(ResourceTask<InstanceLocator>))
            {
                var fingerprint = CreateSpecificationFingerprint();
                return new[] {
                    ResourceTask<InstanceLocator>.ProvisionOnce(
                        parameter.Method,
                        fingerprint,
                        () => InstanceFactory.CreateOrStartInstanceAsync(
                            fingerprint,
                            this.MachineType,
                            this.ImageFamily,
                            this.PublicIp,
                            this.ServiceAccount,
                            this.Metadata))
                };
            }
            else
            {
                throw new ArgumentException(
                    $"Parameter must be of type {typeof(InstanceLocator).Name}");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class WindowsInstanceAttribute : InstanceAttribute
    {
        public const string DefaultMachineType = "n1-standard-2";

        public const string WindowsServer2019 = "projects/windows-cloud/global/images/family/windows-2019";
        public const string WindowsServerCore2019 = "projects/windows-cloud/global/images/family/windows-2019-core";

        protected override string InstanceNamePrefix => "w";

        public override string ImageFamily { get; set; } = WindowsServerCore2019;

        public WindowsInstanceAttribute()
        {
            this.MachineType = DefaultMachineType;
        }

        protected override IEnumerable<Metadata.ItemsData> Metadata
        {
            get
            {
                var key = CreateUniqueGuestAttributeKey();

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
                    Key = InstanceFactory.GuestAttributeToAwaitKey,
                    Value = key
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "windows-startup-script-ps1",
                    Value = "Invoke-RestMethod " +
                        "-Headers @{\"Metadata-Flavor\"=\"Google\"} " +
                        "-Method PUT " +
                        "-Uri http://metadata.google.internal/computeMetadata/v1/instance/" +
                        $"guest-attributes/{InstanceFactory.GuestAttributeNamespace}/{key} " +
                        "-Body TRUE"
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "enable-os-inventory",
                    Value = this.EnableOsInventory ? "TRUE" : "FALSE"
                };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class LinuxInstanceAttribute : InstanceAttribute
    {
        public const string DefaultMachineType = "f1-micro";
        public const string Debian = "projects/debian-cloud/global/images/family/debian-11";

        protected override string InstanceNamePrefix => "u";
        public override string ImageFamily { get; set; } = Debian;

        public LinuxInstanceAttribute()
        {
            this.MachineType = DefaultMachineType;
        }

        protected override IEnumerable<Metadata.ItemsData> Metadata
        {
            get
            {
                var key = CreateUniqueGuestAttributeKey();

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
                    Key = InstanceFactory.GuestAttributeToAwaitKey,
                    Value = key
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "startup-script",
                    Value = script +
                        "sleep 5 && curl -X PUT --data \"TRUE\" " +
                        "http://metadata.google.internal/computeMetadata/v1/instance/" +
                        $"guest-attributes/{InstanceFactory.GuestAttributeNamespace}/{key} " +
                        "-H \"Metadata-Flavor: Google\""
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "enable-os-inventory",
                    Value = this.EnableOsInventory ? "TRUE" : "FALSE"
                };
                yield return new Metadata.ItemsData()
                {
                    Key = "enable-oslogin",
                    Value = this.EnableOsLogin ? "TRUE" : "FALSE"
                };

            }
        }
    }

    public enum InstanceServiceAccount
    {
        None = 0,
        ComputeDefault = 1
    }
}
