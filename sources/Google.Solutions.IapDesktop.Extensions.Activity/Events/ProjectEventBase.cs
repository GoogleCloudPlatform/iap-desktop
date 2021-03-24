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
using System;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Events
{
    public abstract class ProjectEventBase : EventBase
    {
        public string ProjectId
        {
            get
            {
                //
                // Resource name has the format
                // projects/<project-number-or-id>
                // for project-related events.
                //
                var resourceName = base.LogRecord.ProtoPayload.ResourceName;
                if (string.IsNullOrEmpty(resourceName))
                {
                    return null;
                }

                var parts = resourceName.Split('/');
                if (parts.Length != 2)
                {
                    throw new ArgumentException(
                        "Enountered unexpected resource name format: " +
                        base.LogRecord.ProtoPayload.ResourceName);
                }

                return parts[1];
            }
        }

        protected ProjectEventBase(LogRecord logRecord)
            : base(logRecord)
        {
        }
    }
}
