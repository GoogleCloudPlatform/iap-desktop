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

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// Common base interface for commands.
    /// </summary>
    public interface ICommandBase
    {
        /// <summary>
        /// ID, used for tracing.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Caption, shown in UI.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Caption when command is executing, shown in error dialogs.
        /// </summary>
        string ActivityText { get; }
    }
}
