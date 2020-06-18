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

using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using System.Diagnostics;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle
{
    public class SuspendInstanceEvent : LifecycleEventBase, IInstanceStateChangeEvent
    {
        public const string Method = "v1.compute.instances.suspend"; 
        public const string BetaMethod = "beta.compute.instances.suspend";
        public const string AlphaMethod = "alpha.compute.instances.suspend";

        protected override string SuccessMessage => "Instance suspended";
        protected override string ErrorMessage => "Suspending instance failed";

        internal SuspendInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsSuspendInstanceEvent(logRecord));
        }

        public static bool IsSuspendInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                (record.ProtoPayload.MethodName == Method ||
                 record.ProtoPayload.MethodName == BetaMethod ||
                 record.ProtoPayload.MethodName == AlphaMethod);
        }

        //---------------------------------------------------------------------
        // IInstanceStateChangeEvent.
        //---------------------------------------------------------------------

        public bool IsStartingInstance => !IsError;

        public bool IsTerminatingInstance => false;

    }
}
