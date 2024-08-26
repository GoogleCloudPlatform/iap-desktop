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
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using System.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access
{
    public class SetCommonInstanceMetadataEvent : ProjectOperationEventBase
    {
        public const string Method = "v1.compute.projects.setCommonInstanceMetadata";

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
                // Newer setCommonInstanceMetadata (as of late 2021) contain
                // the names of modified fields in the last record,
                // older records don't.
                //
                // NB. Windows user resets never use common instance metadata.
                //
                if (IsModifyingKey("ssh-keys"))
                {
                    return "Linux SSH keys update";
                }
                else
                {
                    return "Linux SSH keys or metadata update";
                }
            }
        }

        internal SetCommonInstanceMetadataEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsSetCommonInstanceMetadataEvent(logRecord));
        }

        public static bool IsSetCommonInstanceMetadataEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                record.ProtoPayload?.MethodName == Method;
        }

        public bool IsModifyingKey(string key)
        {
            return ModifiedMetadata.ExtractModifiedMetadataKeys(
                    this.LogRecord,
                    "projectMetadataDelta")
                .EnsureNotNull()
                .Any(v => v == key);
        }
    }
}
