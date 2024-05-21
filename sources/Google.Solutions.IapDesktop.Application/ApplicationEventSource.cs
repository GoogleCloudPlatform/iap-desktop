//
// Copyright 2023 Google LLC
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

using System.Diagnostics.Tracing;

namespace Google.Solutions.IapDesktop.Application
{
    /// <summary>
    /// ETW event source.
    /// </summary>
    [EventSource(Name = ProviderName, Guid = ProviderGuid)]
    internal sealed class ApplicationEventSource : EventSource
    {
        public const string ProviderName = "Google-IapDesktop-Application";
        public const string ProviderGuid = "4B23296B-C25A-449C-91F2-897BDABAA1A8";

        public static ApplicationEventSource Log { get; } = new ApplicationEventSource();

        //---------------------------------------------------------------------
        // GUI commands.
        //---------------------------------------------------------------------

        public const int CommandExecutedId = 1;
        public const int CommandFailedId = 2;

        [Event(CommandExecutedId, Level = EventLevel.Informational)]
        internal void CommandExecuted(string id)
        {
            WriteEvent(CommandExecutedId, id);
        }

        [Event(CommandFailedId, Level = EventLevel.Warning)]
        internal void CommandFailed(
            string id,
            string type,
            string error,
            string? cause)
        {
            WriteEvent(CommandFailedId, id, type, error, cause);
        }
    }
}
