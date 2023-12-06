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

using Google.Solutions.Apis;
using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Diagnostics.ErrorHandling
{
    public abstract class ThrowExceptionCommandBase
        : MenuCommandBase<DebugMenu.Context>, IMenuCommand<DebugMenu>
    {
        protected ThrowExceptionCommandBase(string text) : base(text)
        {
        }

        protected override bool IsAvailable(DebugMenu.Context context)
        {
            return true;
        }

        protected override bool IsEnabled(DebugMenu.Context context)
        {
            return true;
        }
    }

    //---------------------------------------------------------------------
    // Synchronous exceptions.
    //---------------------------------------------------------------------

    [MenuCommand(typeof(DebugMenu), Rank = 0x300)]
    [Service]
    public class ThrowSyncExceptionWithHelpCommand : ThrowExceptionCommandBase
    {
        public ThrowSyncExceptionWithHelpCommand()
            : base("&Throw ExceptionWithHelp (sync)")
        {
        }

        public override void Execute(DebugMenu.Context context)
        {
            throw new ResourceAccessDeniedException(
                "DEBUG",
                HelpTopics.General,
                new ApplicationException("DEBUG"));
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x301)]
    [Service]
    public class ThrowSyncApplicationExceptionCommand : ThrowExceptionCommandBase
    {
        public ThrowSyncApplicationExceptionCommand()
            : base("&Throw ApplicationException (sync)")
        {
        }

        public override void Execute(DebugMenu.Context context)
        {
            throw new ApplicationException("DEBUG");
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x302)]
    [Service]
    public class ThrowSyncTaskCanceledExceptionCommand : ThrowExceptionCommandBase
    {
        public ThrowSyncTaskCanceledExceptionCommand()
            : base("&Throw TaskCancelledException (sync)")
        {
        }

        public override void Execute(DebugMenu.Context context)
        {
            throw new TaskCanceledException("DEBUG");
        }
    }

    //---------------------------------------------------------------------
    // Asynchronous exceptions.
    //---------------------------------------------------------------------

    [MenuCommand(typeof(DebugMenu), Rank = 0x310)]
    [Service]
    public class ThrowAsyncApplicationExceptionCommand : ThrowExceptionCommandBase
    {
        public ThrowAsyncApplicationExceptionCommand()
            : base("&Throw ApplicationException (async)")
        {
        }

        public override async Task ExecuteAsync(DebugMenu.Context context)
        {
            await Task.Yield();
            throw new ApplicationException("DEBUG");
        }
    }

    [MenuCommand(typeof(DebugMenu), Rank = 0x311)]
    [Service]
    public class ThrowAsyncTaskCanceledExceptionCommand : ThrowExceptionCommandBase
    {
        public ThrowAsyncTaskCanceledExceptionCommand()
            : base("&Throw TaskCancelledException (async)")
        {
        }

        public override async Task ExecuteAsync(DebugMenu.Context context)
        {
            await Task.Yield();
            throw new TaskCanceledException("DEBUG");
        }
    }

    //---------------------------------------------------------------------
    // Unhandled exceptions.
    //---------------------------------------------------------------------

    [MenuCommand(typeof(DebugMenu), Rank = 0x320)]
    [Service]
    public class ThrowWindowExceptionCommand : ThrowExceptionCommandBase
    {
        private readonly IMainWindow mainWindow;

        public ThrowWindowExceptionCommand(IMainWindow mainWindow)
            : base("&Throw unhandled exception")
        {
            this.mainWindow = mainWindow;
        }

        public override void Execute(DebugMenu.Context context)
        {
            ((Control)this.mainWindow).BeginInvoke(
                (Action)(() => throw new ApplicationException("DEBUG")));
        }
    }
}
