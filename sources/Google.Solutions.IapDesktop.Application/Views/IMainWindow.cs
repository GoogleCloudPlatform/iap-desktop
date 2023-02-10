﻿//
// Copyright 2020 Google LLC
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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.Mvvm.Commands;
using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Views
{
    public interface IMainWindow : IWin32Window
    {
        /// <summary>
        /// Dock panel of main window.
        /// </summary>
        DockPanel MainPanel { get; }

        /// <summary>
        /// Close window and exit application.
        /// </summary>
        void Close();

        /// <summary>
        /// Minimize the main window.
        /// </summary>
        void Minimize();

        ICommandContainer<IMainWindow> ViewMenu { get; }

        /// <summary>
        /// Add an item to the main menu.
        /// </summary>
        ICommandContainer<TContext> AddMenu<TContext>(
            string caption,
            int? index,
            Func<TContext> queryCurrentContextFunc)
            where TContext : class;

        void SetUrlHandler(IIapUrlHandler handler);
    }
}
