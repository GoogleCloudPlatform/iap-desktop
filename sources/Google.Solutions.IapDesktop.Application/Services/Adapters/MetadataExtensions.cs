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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Util;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{
    public static class MetadataExtensions
    {
        public static Metadata.ItemsData GetItem(this Metadata metadata, string key)
        {
            return metadata?.Items
                .EnsureNotNull()
                .FirstOrDefault(item =>
                    item.Key != null &&
                    item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetValue(this Metadata metadata, string key)
        {
            return GetItem(metadata, key)?.Value;
        }

        public static bool? GetFlag(this Metadata metadata, string flag)
        {
            var value = metadata?.GetValue(flag);

            if (value == null)
            {
                //
                // Undefined.
                //
                return null;
            }
            else
            {
                //
                // Evaluate "truthyness" using same rules as
                // CheckMetadataFeatureEnabled()
                //
                switch (value.Trim().ToLower())
                {
                    case "true":
                    case "1":
                    case "y":
                    case "yes":
                        return true;

                    case "false":
                    case "0":
                    case "n":
                    case "no":
                        return false;

                    default:
                        return null;
                }
            }
        }

        public static bool? GetFlag(this Instance instance, Project project, string flag)
        {
            //
            // NB. The instance value always takes precedence,
            // even if it's false.
            //

            var instanceValue = instance.Metadata.GetFlag(flag);
            if (instanceValue != null)
            {
                return instanceValue.Value;
            }

            var projectValue = project.CommonInstanceMetadata.GetFlag(flag);
            if (projectValue != null)
            {
                return projectValue.Value;
            }

            return null;
        }

        public static bool? GetFlag(this Project project, string flag)
        {
            return project.CommonInstanceMetadata.GetFlag(flag);
        }
    }
}
