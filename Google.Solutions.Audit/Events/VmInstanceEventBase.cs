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

using Google.Solutions.Audit.Records;
using Google.Solutions.Compute;
using System;

namespace Google.Solutions.Audit.Events
{
    public abstract class VmInstanceEventBase : EventBase
    {
        public VmInstanceReference InstanceReference
        {
            get
            {
                //
                // Resource name has the format
                // projects/<project-number-or-id>/zones/<zone>/instances/<name>
                // for instance-related events.
                //

                var parts = base.LogRecord.ProtoPayload.ResourceName.Split('/');
                if (parts.Length != 6)
                {
                    throw new ArgumentException(
                        "Enountered unexpected resource name format: " +
                        base.LogRecord.ProtoPayload.ResourceName);
                }

                return new VmInstanceReference(
                    base.LogRecord.ProjectId,
                    parts[3],
                    parts[5]);
            }
        }

        public long InstanceId => long.Parse(base.LogRecord.Resource.Labels["instance_id"]);

        protected VmInstanceEventBase(LogRecord logRecord)
            : base(logRecord)
        {
        }
    }
}
