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

using Google.Solutions.Common.Test;
using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Application.Test.Services.Persistence
{
    [TestFixture]
    public class TestVmInstanceConnectionSettings
    {
        [Test]
        public void WhenOverlayUsesDefaults_ThenBaseSettingsPrevail()
        {
            var baseSettings = new VmInstanceConnectionSettings(
                "instance-1",
                "user",
                null,
                "domain",
                RdpConnectionBarState.Off,
                RdpDesktopSize.ScreenSize,
                RdpAuthenticationLevel.RequireServerAuthentication,
                RdpColorDepth.DeepColor,
                RdpAudioMode.PlayOnServer,
                RdpRedirectClipboard.Disabled,
                RdpCredentialGenerationBehavior.Force,
                30,
                60,
                13389);

            var overlay = new VmInstanceConnectionSettings(
                "instance-1",
                null,
                null,
                null,
                RdpConnectionBarState._Default,
                RdpDesktopSize._Default,
                RdpAuthenticationLevel._Default,
                RdpColorDepth._Default,
                RdpAudioMode._Default,
                RdpRedirectClipboard._Default,
                RdpCredentialGenerationBehavior._Default,
                VmInstanceConnectionSettings.DefaultConnectionTimeout,
                VmInstanceConnectionSettings.DefaultIdleTimeoutMinutes,
                VmInstanceConnectionSettings.DefaultRdpPort);

            AssertEx.ArePropertiesEqual(baseSettings, baseSettings.OverlayBy(overlay));
        }

        [Test]
        public void WhenOverlayUsesNonDefaults_ThenOverlaySettingsPrevail()
        {
            var baseSettings = new VmInstanceConnectionSettings(
                "instance-1",
                null,
                null,
                null,
                RdpConnectionBarState._Default,
                RdpDesktopSize._Default,
                RdpAuthenticationLevel._Default,
                RdpColorDepth._Default,
                RdpAudioMode._Default,
                RdpRedirectClipboard._Default,
                RdpCredentialGenerationBehavior._Default,
                30,
                60,
                13389);

            var overlay = new VmInstanceConnectionSettings(
                "instance-1",
                "user",
                null,
                "domain",
                RdpConnectionBarState.Off,
                RdpDesktopSize.ScreenSize,
                RdpAuthenticationLevel.RequireServerAuthentication,
                RdpColorDepth.DeepColor,
                RdpAudioMode.PlayOnServer,
                RdpRedirectClipboard.Disabled,
                RdpCredentialGenerationBehavior.Force,
                130,
                160,
                23389);

            AssertEx.ArePropertiesEqual(overlay, baseSettings.OverlayBy(overlay));
        }

        [Test]
        public void WhenInstanceNamesDiffer_ThenOverlayByThrowsArgumentException()
        {
            var baseSettings = new VmInstanceConnectionSettings(
                "instance-1",
                null,
                null,
                null,
                RdpConnectionBarState._Default,
                RdpDesktopSize._Default,
                RdpAuthenticationLevel._Default,
                RdpColorDepth._Default,
                RdpAudioMode._Default,
                RdpRedirectClipboard._Default,
                RdpCredentialGenerationBehavior._Default,
                VmInstanceConnectionSettings.DefaultConnectionTimeout,
                VmInstanceConnectionSettings.DefaultIdleTimeoutMinutes,
                VmInstanceConnectionSettings.DefaultRdpPort);

            var overlay = new VmInstanceConnectionSettings(
                "instance-2",
                null,
                null,
                null,
                RdpConnectionBarState._Default,
                RdpDesktopSize._Default,
                RdpAuthenticationLevel._Default,
                RdpColorDepth._Default,
                RdpAudioMode._Default,
                RdpRedirectClipboard._Default,
                RdpCredentialGenerationBehavior._Default,
                VmInstanceConnectionSettings.DefaultConnectionTimeout,
                VmInstanceConnectionSettings.DefaultIdleTimeoutMinutes,
                VmInstanceConnectionSettings.DefaultRdpPort);

            Assert.Throws<ArgumentException>(() => baseSettings.OverlayBy(overlay));
        }
    }
}
