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

using Google.Solutions.IapDesktop.Application.Services.ProjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Views.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.Views.Properties;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Testing.Application.ObjectModel;
using Google.Solutions.Testing.Application.Views;
using Moq;
using NUnit.Framework;
using System;
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

        public class SampleViewModel<T> : ViewModelBase, IPropertiesInspectorViewModel
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

            public Task SwitchToModelAsync(IProjectModelNode node)
            {
                throw new System.NotImplementedException();
            }
        }

        public class SampleView<T>
            : PropertiesInspectorViewBase, IView<SampleViewModel<T>>
        {
            public SampleView(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public void Bind(SampleViewModel<T> viewModel)
            {
                base.Bind(viewModel);
            }
        }

        [SetUp]
        public void SetUpServices()
        {
            this.ServiceRegistry.AddMock<IProjectExplorer>();
            this.ServiceRegistry.AddMock<IThemeService>()
                .SetupGet(t => t.ToolWindowTheme)
                .Returns(new Mock<IControlTheme>().Object);
        }

        [Test]
        public void WhenObjectIsPocoWithNoBrowsableProperties_ThenNoPropertiesShown()
        {
            var viewModel = new SampleViewModel<PocoWithoutProperty>();
            var window = new SampleView<PocoWithoutProperty>(this.ServiceProvider);
            window.Bind(viewModel);

            viewModel.InspectedObject = new PocoWithoutProperty();

            window.Show();
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
            var viewModel = new SampleViewModel<PocoWithoutProperty>();
            var window = new SampleView<PocoWithoutProperty>(this.ServiceProvider);
            window.Bind(viewModel);

            viewModel.InspectedObject = new PocoWithProperty();

            window.Show();
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
            var viewModel = new SampleViewModel<Settings>();
            var window = new SampleView<Settings>(this.ServiceProvider);
            window.Bind(viewModel);

            viewModel.InspectedObject = new Settings();

            window.Show();
            PumpWindowMessages();

            var grid = window
                .GetAllControls()
                .OfType<PropertyGrid>()
                .First();
            Assert.AreEqual(1, grid.SelectedGridItem.Parent.GridItems.Count);
        }
    }
}
