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

using Google.Solutions.IapDesktop.Application.Services.Windows.SettingsEditor;
using NUnit.Framework;
using System.ComponentModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestSettingsEditor : WindowTestFixtureBase
    {

        [Test]
        public void WhenObjectHasNoBrowsableProperties_ThenNoPropertiesShown()
        {
            var settings = new EmptyMockSettingsObject();

            var window = new SettingsEditorWindow(this.serviceProvider);
            window.ShowWindow(settings);
            PumpWindowMessages();

            var grid = window.GetChild<PropertyGrid>("propertyGrid");
            Assert.IsNull(grid.SelectedGridItem.Parent);
        }

        [Test]
        public void WhenObjectHasBrowsableProperties_ThenPropertiyIsShown()
        {
            var settings = new MockSettingsObject();

            var window = new SettingsEditorWindow(this.serviceProvider);
            window.ShowWindow(settings);
            PumpWindowMessages();

            var grid = window.GetChild<PropertyGrid>("propertyGrid");
            Assert.AreEqual(1, grid.SelectedGridItem.Parent.GridItems.Count);
        }

        class EmptyMockSettingsObject : ISettingsObject
        {
            public int SaveChangesCalls { get; private set; } = 0;

            public void SaveChanges()
            {
                this.SaveChangesCalls++;
            }

            public string InformationText => null;
        }

        class MockSettingsObject : ISettingsObject
        {
            public int SaveChangesCalls { get; private set; } = 0;

            public void SaveChanges()
            {
                this.SaveChangesCalls++;
            }

            public string InformationText => null;

            [BrowsableSetting]
            [Browsable(true)]
            public string SampleProperty { get; set; }
        }
    }
}
