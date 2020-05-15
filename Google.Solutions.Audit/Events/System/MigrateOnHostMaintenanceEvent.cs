﻿//
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
using System.Diagnostics;

namespace Google.Solutions.Audit.Events.System
{
    public class MigrateOnHostMaintenanceEvent : VmInstanceEventBase
    {
        public const string Method = "compute.instances.migrateOnHostMaintenance";

        public string Message => "Instance migrated during Compute Engine maintenance";

        internal MigrateOnHostMaintenanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsMigrateOnHostMaintenanceEvent(logRecord));
        }

        public static bool IsMigrateOnHostMaintenanceEvent(LogRecord logRecord)
        {
            return logRecord.IsSystemEvent &&
                logRecord.ProtoPayload.MethodName == Method;
        }
    }
}
