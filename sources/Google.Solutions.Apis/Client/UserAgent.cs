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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;

namespace Google.Solutions.Apis.Client
{
    public class UserAgent
    {
        /// <summary>
        /// Product name.
        /// </summary>
        public string Product { get; }

        /// <summary>
        /// Product version.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Operating system and other information about the platform.
        /// </summary>
        public string Platform { get; }

        /// <summary>
        /// Application-specific features/extensions to be added to header.
        /// </summary>
        public string? Extensions { get; set; }

        public UserAgent(string product, Version version, string platform)
        {
            this.Product = product.ExpectNotEmpty(nameof(product));
            this.Version = version;
            this.Platform = platform.ExpectNotEmpty(nameof(platform));
        }

        /// <summary>
        /// Create a value that can be used as "application name" for 
        /// Google API requests. Application names can't contain parentheses
        /// and other special characters, so we can't use the normal header.
        /// </summary>
        public string ToApplicationName()
        {
            return $"{this.Product}/{this.Version}";
        }

        /// <summary>
        /// Create header value that complies with Browser conventions, see
        /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent
        /// </summary>
        public override string ToString()
        {
            var platform = this.Platform;

            if (!string.IsNullOrEmpty(this.Extensions))
            {
                Debug.Assert(this.Extensions!.IndexOf(';') < 0);
                platform += "; " + this.Extensions;
            }

            return $"{ToApplicationName()} ({platform}) CLR/{ClrVersion.Version}";
        }
    }
}
