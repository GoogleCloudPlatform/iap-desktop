//
// Copyright 2024 Google LLC
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

using Google.Solutions.Mvvm.Theme;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Theme for system dialogs and other secondary windows.
    /// </summary>
    public interface ISystemDialogTheme : IControlTheme { }

    /// <summary>
    /// Theme for dialogs and other secondary windows.
    /// </summary>
    public interface IDialogTheme : IControlTheme { }

    /// <summary>
    /// Theme for tool windows, docked or undocked.
    /// </summary>
    public interface IToolWindowTheme : IControlTheme { }

    /// <summary>
    /// Theme for the main window.
    /// </summary>
    public interface IMainWindowTheme : IControlTheme
    {
        /// <summary>
        /// Theme for the docking suite.
        /// </summary>
        ThemeBase DockPanelTheme { get; }
    }
}
