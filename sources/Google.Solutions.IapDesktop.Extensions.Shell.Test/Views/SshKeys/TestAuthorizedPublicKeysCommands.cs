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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh;
using Google.Solutions.IapDesktop.Extensions.Shell.Views.SshKeys;
using Google.Solutions.Mvvm.Binding.Commands;
using Google.Solutions.Testing.Application.ObjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Test.Views.SshKeys
{
    [TestFixture]
    public class TestAuthorizedPublicKeysCommands
    {
        //---------------------------------------------------------------------
        // ContextMenuOpen.
        //---------------------------------------------------------------------

        [Test]
        public void WhenApplicable_ThenContextMenuOpenIsEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var context = new Mock<IProjectModelProjectNode>();

            var commands = new AuthorizedPublicKeysCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled, 
                commands.ContextMenuOpen.QueryState(context.Object));
        }

        [Test]
        public void WhenNotApplicable_ThenContextMenuOpenIsUnavailable()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var context = new Mock<IProjectModelNode>();

            var commands = new AuthorizedPublicKeysCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Unavailable, 
                commands.ContextMenuOpen.QueryState(context.Object));
        }

        //---------------------------------------------------------------------
        // WindowMenuOpen.
        //---------------------------------------------------------------------

        [Test]
        public void WindowMenuOpenIsEnabled()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var context = new Mock<IMainWindow>();

            var commands = new AuthorizedPublicKeysCommands(serviceProvider.Object);

            Assert.AreEqual(
                CommandState.Enabled,
                commands.WindowMenuOpen.QueryState(context.Object));
        }
    }
}
