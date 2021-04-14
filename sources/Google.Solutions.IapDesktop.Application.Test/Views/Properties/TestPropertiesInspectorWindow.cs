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
using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Test.ObjectModel;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CA1034 // Nested types should not be visible

namespace Google.Solutions.IapDesktop.Application.Test.Views.Properties
{
    [TestFixture]
    public class TestPropertiesInspectorWindow : WindowTestFixtureBase
    {
        public class PocoWithoutProperty
        {
        }

        public class PocoWithProperty
        {
            [Browsable(true)]
            public string Name { get; set; }
        }

        public class Settings : ISettingsCollection
        {
            IEnumerable<ISetting> ISettingsCollection.Settings => new ISetting[]
            {
                RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    null,
                    null,
                    _ => true)
            };
        }

        public class ViewModel<T> : ViewModelBase, IPropertiesInspectorViewModel
        {
            private object inspectedObject;

            public bool IsInformationBarVisible => false;

            public string InformationText => null;

            public object InspectedObject
            {
                get => this.inspectedObject;
                set
                {
                    this.inspectedObject = value;
                    RaisePropertyChange();
                }
            }

            public string WindowTitle => "";

            public void SaveChanges()
            {
                throw new System.NotImplementedException();
            }

            public Task SwitchToModelAsync(IProjectExplorerNode node)
            {
                throw new System.NotImplementedException();
            }
        }

        [Test]
        public void WhenObjectIsPocoWithNoBrowsableProperties_ThenNoPropertiesShown()
        {
            this.serviceRegistry.AddMock<IProjectExplorer>();
            var viewModel = new ViewModel<PocoWithoutProperty>();

            var window = new PropertiesInspectorWindow(this.serviceProvider, viewModel);
            viewModel.InspectedObject = new PocoWithoutProperty();

            window.ShowWindow();
            PumpWindowMessages();

            var grid = window
                .GetAllControls()
                .OfType<PropertyGrid>()
                .First();
            Assert.IsNull(grid.SelectedGridItem.Parent);
        }

        [Test]
        public void WhenObjectIsPocoWithBrowsableProperty_ThenPropertyIsShown()
        {
            this.serviceRegistry.AddMock<IProjectExplorer>();
            var viewModel = new ViewModel<PocoWithoutProperty>();

            var window = new PropertiesInspectorWindow(this.serviceProvider, viewModel);
            viewModel.InspectedObject = new PocoWithProperty();

            window.ShowWindow();
            PumpWindowMessages();

            var grid = window
                .GetAllControls()
                .OfType<PropertyGrid>()
                .First();
            Assert.AreEqual(1, grid.SelectedGridItem.Parent.GridItems.Count);
        }

        [Test]
        public void WhenObjectIsSettingsCollection_ThenSettingIsShown()
        {
            this.serviceRegistry.AddMock<IProjectExplorer>();
            var viewModel = new ViewModel<Settings>();

            var window = new PropertiesInspectorWindow(this.serviceProvider, viewModel);
            viewModel.InspectedObject = new Settings();

            window.ShowWindow();
            PumpWindowMessages();

            var grid = window
                .GetAllControls()
                .OfType<PropertyGrid>()
                .First();
            Assert.AreEqual(1, grid.SelectedGridItem.Parent.GridItems.Count);
        }
    }
}
