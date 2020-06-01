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

using Google.Apis.Compute.v1;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.LogAnalysis.History;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
{
    /// <summary>
    /// Model for the report. A Report archive can be saved and loaded from disk
    /// so that it can be analyzed again later.
    /// </summary>
    public class ReportArchive
    {
        [JsonProperty("history")]
        public InstanceSetHistory History { get; }

        [JsonProperty("licenseAnnotations")]
        internal IDictionary<string, ImageAnnotation> LicenseAnnotations { get; }

        [JsonConstructor]
        internal ReportArchive(
            [JsonProperty("history")] InstanceSetHistory instanceSet,
            [JsonProperty("licenseAnnotations")] IDictionary<string, ImageAnnotation> annotations)
        {
            this.History = instanceSet;
            this.LicenseAnnotations = annotations;
        }

        internal ReportArchive(InstanceSetHistory instanceSet)
            :this(instanceSet, new Dictionary<string, ImageAnnotation>())
        {
        }

        internal bool IsInstanceAnnotatedAs(
            InstanceHistory instance,
            OperatingSystemTypes osTypes,
            LicenseTypes licenseTypes)
        {
            ImageAnnotation annotation;
            if (instance.Image == null ||
                !this.LicenseAnnotations.TryGetValue(instance.Image.ToString(), out annotation))
            {
                annotation = ImageAnnotation.Default;
            }

            Debug.Assert(annotation != null);

            return (osTypes.HasFlag(annotation.OperatingSystem) &&
                licenseTypes.HasFlag(annotation.LicenseType));
        }

        public virtual IEnumerable<InstanceHistory> GetInstances(
            OperatingSystemTypes osTypes,
            LicenseTypes licenseTypes)
        {
            return this.History.Instances
                .Where(i => IsInstanceAnnotatedAs(i, osTypes, licenseTypes));
        }

        public static ReportArchive FromInstanceSetHistory(
            InstanceSetHistory instanceSet)
        {
            return new ReportArchive(
                instanceSet,
                new Dictionary<string, ImageAnnotation>());
        }

        public void AddLicenseAnnotation(
            ImageLocator image, 
            LicenseLocator license)
        {
            this.LicenseAnnotations[image.ToString()] = ImageAnnotation.FromLicense(license);
        }

        public void AddLicenseAnnotation(
            ImageLocator image, 
            OperatingSystemTypes osType, 
            LicenseTypes licenseType)
        {
            this.LicenseAnnotations[image.ToString()] = new ImageAnnotation(osType, licenseType);
        }

        public async Task LoadLicenseAnnotationsAsync(
            IComputeEngineAdapter computeEngineAdapter,
            CancellationToken cancellationToken)
        {
            await LicenseLoader.LoadLicenseAnnotationsAsync(
                this,
                computeEngineAdapter, 
                cancellationToken);
        }

        //---------------------------------------------------------------------
        // Serialization.
        //---------------------------------------------------------------------

        internal class Envelope
        {
            internal const string TypeAnnotation = "type.googleapis.com/Google.Solutions.IapDesktop.Extensions.LogAnalysis.AnnotatedInstanceSetHistory";

            [JsonProperty("@type")]
            internal string Type => TypeAnnotation;

            [JsonProperty("annotatedInstanceSetHistory")]
            internal ReportArchive InstanceSetHistory { get; }

            public Envelope(
                ReportArchive instanceSetHistory)
            {
                this.InstanceSetHistory = instanceSetHistory;
            }

            [JsonConstructor]
            public Envelope(
                [JsonProperty("@type")] string typeAnnotation,
                [JsonProperty("instanceSetHistory")] ReportArchive instanceSetHistory)
                : this(instanceSetHistory)
            {
                if (typeAnnotation != TypeAnnotation)
                {
                    throw new FormatException("Missing type annotation: " + TypeAnnotation);
                }
            }
        }

        private static JsonSerializer CreateSerializer()
        {
            return JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    NullValueHandling = NullValueHandling.Ignore
                });
        }

        public void Serialize(TextWriter writer)
        {
            CreateSerializer().Serialize(writer, new Envelope(this));
        }

        public static ReportArchive Deserialize(TextReader reader)
        {
            return CreateSerializer().Deserialize<ReportArchive.Envelope>(
                new JsonTextReader(reader)).InstanceSetHistory;
        }
    }
}
