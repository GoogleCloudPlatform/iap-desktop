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

using Google.Solutions.Common;
using Google.Solutions.LogAnalysis.Logs;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Google.Solutions.LogAnalysis.Events.Lifecycle
{
    public class InsertInstanceEvent : LifecycleEventBase, IInstanceStateChangeEvent
    {
        public const string Method = "v1.compute.instances.insert";
        public const string BetaMethod = "beta.compute.instances.insert";

        protected override string SuccessMessage => "Instance created";
        protected override string ErrorMessage => "Creating instance failed";

        public GlobalResourceReference Image { get; }

        internal InsertInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsInsertInstanceEvent(logRecord));

            var request = logRecord.ProtoPayload.Request;
            if (request != null)
            {
                var disks = request["disks"];
                if (disks != null)
                {
                    foreach (var disk in ((JArray)disks))
                    {
                        if (disk["boot"] != null && (bool)disk["boot"])
                        {
                            var initializeParams = disk["initializeParams"];
                            if (initializeParams != null)
                            {
                                this.Image = GlobalResourceReference.FromString(
                                    initializeParams["sourceImage"].Value<string>());
                            }
                        }
                    }
                }
            }
        }

        public static bool IsInsertInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                (record.ProtoPayload.MethodName == Method ||
                 record.ProtoPayload.MethodName == BetaMethod);
        }


        //---------------------------------------------------------------------
        // IInstanceStateChangeEvent.
        //---------------------------------------------------------------------

        public bool IsStartingInstance => !this.IsError;

        public bool IsTerminatingInstance => false;
    }
}