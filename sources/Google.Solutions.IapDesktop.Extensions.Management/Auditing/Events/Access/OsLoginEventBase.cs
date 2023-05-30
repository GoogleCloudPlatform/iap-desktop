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
    /// <summary>
    /// Base class for OS Login events.
    /// </summary>
    public abstract class OsLoginEventBase : InstanceEventBase
    {
        internal OsLoginEventBase(LogRecord logRecord) : base(logRecord)
        {
        }

        public override ulong InstanceId
        {
            get
            {
                //
                // Unlike just about all other logs, OS Login logs
                // have the zone and instance_id at the top level:
                //
                // "labels": {
                //   "zone": "europe-west4-a",
                //   "instance_id": "1234567890"
                // }
                //

                var id = this.LogRecord.Labels?["instance_id"];
                return id == null ? 0 : ulong.Parse(id);
            }
        }
    }
}
