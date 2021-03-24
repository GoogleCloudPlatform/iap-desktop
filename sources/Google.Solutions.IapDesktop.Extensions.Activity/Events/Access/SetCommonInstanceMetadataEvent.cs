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

using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events.Access
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
                // NB. Unlike setMetadata records, the setCommonInstanceMetadata records
                // do not contain the names of modified keys (b/170386936). Therefore, we
                // cannot say whether this was a Linux SSH keys update or something else.
                // 
                // NB. Windows user resets never use common instance metadata.
                //
                return "Linux SSH keys or metadata update";
            }
        }

        internal SetCommonInstanceMetadataEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsSetCommonInstanceMetadataEvent(logRecord));
        }

        public static bool IsSetCommonInstanceMetadataEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                record.ProtoPayload.MethodName == Method;
        }
    }
}
