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

using Google.Solutions.Logging.Records;
using System.Diagnostics;

namespace Google.Solutions.Logging.Events
{
    public class StopInstanceEvent : VmInstanceEventBase
    {
        public const string BetaMethod = "beta.compute.instances.stop";
        public const string Method = "v1.compute.instances.stop";

        public StopInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsStopInstanceEvent(logRecord));
        }

        public static bool IsStopInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                (record.ProtoPayload.MethodName == BetaMethod ||
                 record.ProtoPayload.MethodName == Method);
        }
    }
}
