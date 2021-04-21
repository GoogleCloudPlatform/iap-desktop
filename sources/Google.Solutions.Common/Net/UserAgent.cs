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

using System;

namespace Google.Solutions.Common.Net
{
    public class UserAgent
    {
        public string Product { get; }
        public Version Version { get; }
        public string Platform { get; }

        private static string OsVersion
        {
            get
            {
                var version = Environment.OSVersion.VersionString;
                if (!Environment.Is64BitOperatingSystem)
                {
                    version += " 32-bit";
                }

                return version;
            }
        }

        internal UserAgent(string product, Version version, string platform)
        {
            this.Product = product;
            this.Version = version;
            this.Platform = platform;
        }

        public UserAgent(string product, Version version)
            : this(product, version, OsVersion)
        {
        }

        /// <summary>
        /// Create a value that can be used as "application name" for 
        /// Google API requests.
        /// </summary>
        public string ToApplicationName()
            => $"{this.Product}/{this.Version}";

        /// <summary>
        /// Create header value that complies with Browser conventions, see
        /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent
        /// </summary>
        public string ToHeaderValue()
            => $"{this.Product}/{this.Version} ({this.Platform})";

        public override string ToString() => ToHeaderValue();
    }
}
