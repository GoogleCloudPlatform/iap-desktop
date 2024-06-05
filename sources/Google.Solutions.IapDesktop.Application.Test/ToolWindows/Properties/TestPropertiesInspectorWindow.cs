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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Application.ToolWindows.Properties;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
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

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.Properties
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
            public string? Name { get; set; }
        }

        public class Settings : ISettingsCollection
        {
            IEnumerable<ISetting> ISettingsCollection.Settings => new ISetting[]
            {
                new DictionarySettingsStore(new Dictionary<string, string>())
                .Read<string?>(
                    "test",
                    "title",
                    "description",
                    "category",
                    null,
                    _ => true)
            };
        }

        public class SampleViewModel<T> : ViewModelBase, IPropertiesInspectorViewModel
        {
            public readonly ObservableProperty<string?> informationText = ObservableProperty.Build<string?>(null);
            public readonly ObservableProperty<object?> inspectedObject = ObservableProperty.Build<object?>(null);
            public readonly ObservableProperty<string> windowTitle = ObservableProperty.Build<string>("");

            public IObservableProperty<string?> InformationText => this.informationText;
            public IObservableProperty<object?> InspectedObject => this.inspectedObject;
            public IObservableProperty<string> WindowTitle => this.windowTitle;

            public void SaveChanges()
            {
                throw new NotImplementedException();
            }

            public Task SwitchToModelAsync(IProjectModelNode node)
            {
                throw new NotImplementedException();
            }
        }

        public class SampleView<T>
            : PropertiesInspectorViewBase, IView<SampleViewModel<T>>
        {
            public SampleView(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public void Bind(
                SampleViewModel<T> viewModel,
                IBindingContext bindingContext)
            {
                base.Bind(viewModel, bindingContext);
            }
        }

        [SetUp]
        public void SetUpServices()
        {
            this.ServiceRegistry.AddMock<IProjectExplorer>();
            this.ServiceRegistry.AddMock<IToolWindowTheme>();
        }

        [Test]
        public void WhenObjectIsPocoWithNoBrowsableProperties_ThenNoPropertiesShown()
        {
            var viewModel = new SampleViewModel<PocoWithoutProperty>();
            var window = new SampleView<PocoWithoutProperty>(this.ServiceProvider);
            window.Bind(
                viewModel,
                new Mock<IBindingContext>().Object);

            viewModel.inspectedObject.Value = new PocoWithoutProperty();

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
            window.Bind(
                viewModel,
                new Mock<IBindingContext>().Object);

            viewModel.inspectedObject.Value = new PocoWithProperty();

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
            window.Bind(
                viewModel,
                new Mock<IBindingContext>().Object);

            viewModel.inspectedObject.Value = new Settings();

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
