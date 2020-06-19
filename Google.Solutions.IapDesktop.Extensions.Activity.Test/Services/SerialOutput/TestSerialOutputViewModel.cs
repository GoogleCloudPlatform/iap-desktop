//
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

using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services.Integration;
using Google.Solutions.IapDesktop.Application.Services.Windows.ProjectExplorer;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.System;
using Google.Solutions.IapDesktop.Extensions.Activity.History;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.EventLog;
using Google.Solutions.IapDesktop.Extensions.Activity.Services.SerialOutput;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Services.EventLog
{
    [TestFixture]
    public class TestSerialOutputViewModel : FixtureBase
    {
        private SerialOutputViewModel viewModel;


        [Test]
        public void WhenNodeIsCloudNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerCloudNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, SerialOutputViewModel.GetCommandState(node));
        }

        [Test]
        public void WhenNodeIsProjectNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerProjectNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, SerialOutputViewModel.GetCommandState(node));
        }

        [Test]
        public void WhenNodeIsZoneNode_ThenCommandStateIsUnavailable()
        {
            var node = new Mock<IProjectExplorerZoneNode>().Object;
            Assert.AreEqual(CommandState.Unavailable, SerialOutputViewModel.GetCommandState(node));
        }

        [Test]
        public void WhenNodeIsVmNodeAndRunning_ThenCommandStateIsEnabled()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.IsRunning).Returns(true);
            Assert.AreEqual(CommandState.Enabled, SerialOutputViewModel.GetCommandState(node.Object));
        }

        [Test]
        public void WhenNodeIsVmNodeAndStopped_ThenCommandStateIsEnabled()
        {
            var node = new Mock<IProjectExplorerVmInstanceNode>();
            node.SetupGet(n => n.IsRunning).Returns(false);
            Assert.AreEqual(CommandState.Disabled, SerialOutputViewModel.GetCommandState(node.Object));
        }

        //---------------------------------------------------------------------
        // Model switching.
        //---------------------------------------------------------------------

        [Test]
        public async Task WhenSwitchingToCloudNode_ThenControlsAreDisabled()
        {
            var node = new Mock<IProjectExplorerCloudNode>();
            await this.viewModel.SwitchToModelAsync(node.Object);

            Assert.IsFalse(this.viewModel.IsPortComboBoxEnabled);
        }

        [Test]
        public void WhenSwitchingToInstanceNode_ThenOutputIsPopulated()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void WhenSwitchingPort_ThenOutputIsPopulated()
        {
            Assert.Inconclusive();
        }
    }
}
