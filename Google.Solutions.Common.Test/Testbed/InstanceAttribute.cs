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
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Test.Testbed
{
    public class InstanceRequest
    {
        public Func<Task<VmInstanceReference>> GetInstanceAsync { get; }
        public VmInstanceReference InstanceReference { get; }

        public InstanceRequest(VmInstanceReference instance, Func<Task<VmInstanceReference>> getnstance)
        {
            this.InstanceReference = instance;
            this.GetInstanceAsync = getnstance;
        }

        public override string ToString()
        {
            return this.InstanceReference.ToString();
        }

        public async Task AwaitReady()
        {
            await GetInstanceAsync();
        }
    }

    public abstract class InstanceAttribute : NUnitAttribute, IParameterDataSource
    {
        protected const string GuestAttributeNamespace = "boot";
        protected const string GuestAttributeKey = "completed";

        public string ProjectId { get; set; } = Defaults.ProjectId;
        public string Zone { get; set; } = Defaults.Zone;
        public string MachineType { get; set; } = "n1-standard-1";
        public string ImageFamily { get; set; }
        public string InitializeScript { get; set; }

        protected abstract string InstanceNamePrefix { get; }
        protected abstract IEnumerable<Metadata.ItemsData> Metadata { get; }

        public string UniqueId
        {
            get
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
        }

        public override string ToString()
        {
            return this.UniqueId;
        }

        public IEnumerable GetData(IParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(InstanceRequest))
            {
                var vmRef = new VmInstanceReference(
                    this.ProjectId,
                    this.Zone,
                    this.UniqueId);
                yield return new InstanceRequest(vmRef, () => GetInstanceAsync(vmRef));
            }
            else
            {
                throw new ArgumentException($"Parameter must be of type {typeof(VmInstanceReference).Name}");
            }
        }

        private async Task<VmInstanceReference> GetInstanceAsync(VmInstanceReference vmRef)
        {
            var computeEngine = ComputeEngine.Connect();

            try
            {
                var instance = await computeEngine.Service.Instances
                    .Get(vmRef.ProjectId, vmRef.Zone, vmRef.InstanceName)
                    .ExecuteAsync();

                if (instance.Status == "STOPPED")
                {
                    await computeEngine.Service.Instances.Start(
                        vmRef.ProjectId, vmRef.Zone, vmRef.InstanceName)
                        .ExecuteAsync();
                }

                await AwaitReady(computeEngine, vmRef);
            }
            catch (Exception)
            {
                var metadata = new List<Metadata.ItemsData>(this.Metadata.ToList());

                // Add metdata that marks this instance as temporary.
                metadata.Add(new Metadata.ItemsData()
                {
                    Key = "type",
                    Value = "auto-cleanup"
                });
                metadata.Add(new Metadata.ItemsData()
                {
                    Key = "ttl",
                    Value = "120" // minutes
                });

                await computeEngine.Service.Instances.Insert(
                    new Apis.Compute.v1.Data.Instance()
                    {
                        Name = vmRef.InstanceName,
                        MachineType = $"zones/{this.Zone}/machineTypes/{this.MachineType}",
                        Disks = new[]
                        {
                            new AttachedDisk()
                            {
                                AutoDelete = true,
                                Boot = true,
                                InitializeParams = new AttachedDiskInitializeParams()
                                {
                                    SourceImage = this.ImageFamily
                                }
                            }
                        },
                        Metadata = new Metadata()
                        {
                            Items = metadata
                        },
                        NetworkInterfaces = new[]
                        {
                            new NetworkInterface()
                            {
                                AccessConfigs = new []
                                {
                                    new AccessConfig()
                                }
                            }
                        }
                    },
                    vmRef.ProjectId,
                    vmRef.Zone).ExecuteAsync();

                await AwaitReady(computeEngine, vmRef);
            }

            return vmRef;
        }

        private Task AwaitReady(ComputeEngine engine, VmInstanceReference instanceRef)
        {
            return Task.Run(async () =>
            {
                for (int i = 0; i < 60; i++)
                {
                    try
                    {
                        var instance = await engine.Service.Instances.Get(
                                instanceRef.ProjectId, instanceRef.Zone, instanceRef.InstanceName)
                            .ExecuteAsync();

                        if (await IsReadyAsync(engine, instanceRef, instance))
                        {
                            return;
                        }
                    }
                    catch (Exception)
                    { }

                    await Task.Delay(5 * 1000);
                }

                throw new TimeoutException($"Timeout waiting for {instanceRef} to become ready");
            });
        }

        protected virtual async Task<bool> IsReadyAsync(
            ComputeEngine engine,
            VmInstanceReference instanceRef,
            Instance instance)
        {
            var request = engine.Service.Instances.GetGuestAttributes(
                    instanceRef.ProjectId,
                    instanceRef.Zone,
                    instanceRef.InstanceName);
            request.QueryPath = GuestAttributeNamespace + "/";
            var guestAttributes = await request.ExecuteAsync();

            return guestAttributes
                .QueryValue
                .Items
                .Where(i => i.Namespace__ == GuestAttributeNamespace && i.Key == GuestAttributeKey)
                .Any();
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
                        $"guest-attributes/{GuestAttributeNamespace}/{GuestAttributeKey} " +
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
                        $"guest-attributes/{GuestAttributeNamespace}/{GuestAttributeKey} " +
                        "-H \"Metadata-Flavor: Google\""
                };
            }
        }
    }
}
