﻿//
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

using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Extensions.Explorer.ToolWindows.Install;
using Google.Solutions.Testing.Application.Test;
using Moq;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Extensions.Explorer.Test.ToolWindows.Install
{
    [TestFixture]
    public class TestAboutViewModel : ApplicationFixtureBase
    {
        [Test]
        public void Information()
        {
            var install = new Mock<IInstall>();
            install
                .SetupGet(s => s.CurrentVersion)
                .Returns(new Version(1, 2, 3, 4));
            var viewModel = new AboutViewModel(install.Object);

            StringAssert.Contains("Version 1.2.3.4", viewModel.Information);
        }

        [Test]
        public void LicenseText()
        {
            var viewModel = new AboutViewModel(new Mock<IInstall>().Object);

            Assert.IsNotNull(viewModel.LicenseText);
            StringAssert.Contains("Apache", viewModel.LicenseText);
        }
    }
}
