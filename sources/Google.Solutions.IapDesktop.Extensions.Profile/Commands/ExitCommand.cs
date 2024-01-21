﻿//
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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Profile.Commands
{
    [MenuCommand(typeof(ProfileMenu), Rank = 0x1000)]
    [Service]
    public class ExitCommand : ProfileMenuCommandBase
    {
        private readonly IMainWindow mainWindow;

        public ExitCommand(IMainWindow mainWindow)
            : base("E&xit")
        {
            this.mainWindow = mainWindow.ExpectNotNull(nameof(mainWindow));

            this.ShortcutKeys = Keys.Alt | Keys.F4;
        }

        public override void Execute(UserProfile _)
        {
            this.mainWindow.Close();
        }
    }
}
