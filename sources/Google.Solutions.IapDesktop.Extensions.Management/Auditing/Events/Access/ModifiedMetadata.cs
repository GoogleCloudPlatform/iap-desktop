//
// Copyright 2022 Google LLC
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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;

namespace Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access
{
    internal static class ModifiedMetadata
    {
        internal static string[]? ExtractModifiedMetadataKeys(
            LogRecord record,
            string fieldName)
        {
            // NB. instanceMetadataDelta contains one of two fields:
            // - modifiedMetadataKeys 
            // - addedMetadataKeys
            // in both cases, the value is an array of metadata keys.
            var delta = record.ProtoPayload?.Metadata?[fieldName];
            var modifiedMetadataKeys = delta?["modifiedMetadataKeys"];
            if (modifiedMetadataKeys != null)
            {
                return modifiedMetadataKeys.ToObject<string[]>();
            }

            var addedMetadataKeys = delta?["addedMetadataKeys"];
            if (addedMetadataKeys != null)
            {
                return addedMetadataKeys.ToObject<string[]>();
            }

            return null;
        }
    }
}
