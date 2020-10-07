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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events.Access
{
    public class ResetWindowsUserEvent : VmInstanceActivityEventBase
    {
        public const string Method = "v1.compute.instances.setMetadata";

        protected override string SuccessMessage => 
            $"Windows credential reset from {this.SourceHost ?? "(unknown)"} " +
            $"using {this.UserAgentShort ?? "(unknown agent)"}";

        protected override string ErrorMessage =>
            $"Metadata or Windows credentials reset from {this.SourceHost ?? "(unknown)"} "+
            $"using {this.UserAgentShort ?? "(unknown agent)"} failed";

        internal ResetWindowsUserEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsResetWindowsUserEvent(logRecord));
        }

        public static bool IsResetWindowsUserEvent(LogRecord record)
        {
            //
            // NB. Windows user resets are regular set-metadata requests - the only
            // characteristic aspect about them is that they update the "windows-keys"
            // metadata item.
            //
            // Operation start-records and successful operation end-records contain
            // the metadata key, so we can clearly identify them as being Windows
            // user reset events. Error records however lack this information - so
            // we if we see a failed set-metadata request, it may or may not be
            // the result of a Windows reset user event.
            //

            return record.IsActivityEvent &&
                record.ProtoPayload.MethodName == Method &&
                record.Severity == "ERROR" || ExtractModifiedMetadataKeys(record)
                    .EnsureNotNull()
                    .Any(v => v == "windows-keys");
        }

        private static string[] ExtractModifiedMetadataKeys(LogRecord record)
        {
            // NB. instanceMetadataDelta contains one of two fields:
            // - modifiedMetadataKeys 
            // - addedMetadataKeys
            // in both cases, the value is an array of metadata keys.
            var delta = record.ProtoPayload.Metadata?["instanceMetadataDelta"];
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
