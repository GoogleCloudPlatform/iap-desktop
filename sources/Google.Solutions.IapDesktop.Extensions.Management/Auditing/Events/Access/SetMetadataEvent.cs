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

using Google.Solutions.Common.Linq;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access
{
    public class SetMetadataEvent : InstanceOperationEventBase
    {
        public const string Method = "v1.compute.instances.setMetadata";

        public override EventCategory Category => EventCategory.Access;

        protected override string SuccessMessage =>
            $"{this.Description} from {this.SourceHost ?? "(unknown)"} " +
            $"using {this.UserAgentShort ?? "(unknown agent)"}";

        protected override string ErrorMessage =>
            $"{this.Description} from {this.SourceHost ?? "(unknown)"} " +
            $"using {this.UserAgentShort ?? "(unknown agent)"} failed";

        private string Description
        {
            get
            {
                //
                // NB. Windows user resets are regular set-metadata requests - the only
                // characteristic aspect about them is that they update the "windows-keys"
                // metadata item. Same for Linux/SSH keys.
                //
                // Operation start-records and successful operation end-records contain
                // the metadata key, so we can clearly identify them as being Windows
                // user reset events. Error records however lack this information - so
                // we if we see a failed set-metadata request, it may or may not be
                // the result of a Windows reset user event.
                //

                if (IsModifyingKey("windows-keys"))
                {
                    return "Windows credential update";
                }
                else if (IsModifyingKey("ssh-keys"))
                {
                    return "Linux SSH keys update";
                }
                else if (this.LogRecord.Severity == "ERROR")
                {
                    return "Metadata, Windows credentials, or SSH key update";
                }
                else
                {
                    return "Metadata update";
                }
            }
        }

        internal SetMetadataEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsSetMetadataEvent(logRecord));
        }

        public static bool IsSetMetadataEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                record.ProtoPayload?.MethodName == Method;
        }

        public bool IsModifyingKey(string key)
        {
            return ModifiedMetadata.ExtractModifiedMetadataKeys(
                    this.LogRecord,
                    "instanceMetadataDelta")
                .EnsureNotNull()
                .Any(v => v == key);
        }
    }
}
