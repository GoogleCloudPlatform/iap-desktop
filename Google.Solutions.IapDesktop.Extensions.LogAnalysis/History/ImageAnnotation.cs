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

using Google.Solutions.Common.Locator;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.History
{
    [Flags]
    public enum LicenseTypes
    {
        Unknown = 1,
        Byol = 2,
        Spla = 4
    }

    [Flags]
    public enum OperatingSystemTypes
    {
        Unknown = 1,
        Windows = 2,
        Linux = 4
    }

    public class ImageAnnotation
    {
        public static readonly ImageAnnotation Default =
            new ImageAnnotation(OperatingSystemTypes.Unknown, LicenseTypes.Unknown);

        [JsonProperty("licenseType")]
        public LicenseTypes LicenseType { get; }

        [JsonProperty("os")]
        public OperatingSystemTypes OperatingSystem { get; }

        [JsonConstructor]
        internal ImageAnnotation(
            [JsonProperty("os")] OperatingSystemTypes osType,
            [JsonProperty("licenseType")] LicenseTypes licenseType)
        {
            this.LicenseType = licenseType;
            this.OperatingSystem = osType;
        }

        internal static ImageAnnotation FromLicense(LicenseLocator license)
        {
            if (license.IsWindowsByolLicense())
            {
                return new ImageAnnotation(
                    OperatingSystemTypes.Windows,
                    LicenseTypes.Byol);
            }
            else if (license.IsWindowsLicense())
            {
                return new ImageAnnotation(
                    OperatingSystemTypes.Windows,
                    LicenseTypes.Spla);
            }
            else if (license != null)
            {
                return new ImageAnnotation(
                    OperatingSystemTypes.Linux,
                    LicenseTypes.Unknown);
            }
            else
            {
                return new ImageAnnotation(
                    OperatingSystemTypes.Unknown,
                    LicenseTypes.Unknown);
            }
        }
    }
}
