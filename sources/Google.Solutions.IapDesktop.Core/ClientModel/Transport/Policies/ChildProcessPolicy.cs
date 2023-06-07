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

using Google.Solutions.Common.Util;
using Google.Solutions.Platform.Dispatch;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Transport.Policies
{
    /// <summary>
    /// Policy that only allows access from child processes.
    /// </summary>
    public class ChildProcessPolicy : ProcessPolicyBase
    {
        private readonly IWin32ProcessSet childProcesses;

        public ChildProcessPolicy(IWin32ProcessSet processFactory)
        {
            this.childProcesses = processFactory.ExpectNotNull(nameof(processFactory));
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string Name => "Child processes";

        protected internal override bool IsClientProcessAllowed(uint processId)
        {
            return this.childProcesses.Contains(processId);
        }
    }
}
