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

using Google.Solutions.Mvvm.Binding.Commands;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Context for MVVM binding operations.
    /// </summary>
    public interface IBindingContext
    {
        /// <summary>
        /// Notify that a command executed successfully.
        /// </summary>
        void OnCommandExecuted(ICommandBase command);

        /// <summary>
        /// Notify that a command failed.
        /// </summary>
        void OnCommandFailed(
            IWin32Window? window,
            ICommandBase command,
            Exception exception);

        /// <summary>
        /// Notify that a new binding has been created. Implementing
        /// classes should dispose the binding when it's no longer needed,
        /// for example by tying them to the lifecycle of the control.
        /// </summary>
        void OnBindingCreated(IComponent control, IDisposable binding);
    }
}
