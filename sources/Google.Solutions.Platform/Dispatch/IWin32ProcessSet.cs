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

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// A transitive set of processes, can be used to track child processes.
    /// </summary>
    public interface IWin32ProcessSet
    {
        /// <summary>
        /// Check if a process is direct or indirect member of this set.
        /// </summary>
        bool Contains(IWin32Process process);

        /// <summary>
        /// Check if a process is direct or indirect member of this set.
        /// </summary>
        bool Contains(uint processId);
    }
}
