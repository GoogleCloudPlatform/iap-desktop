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

using Google.Solutions.IapDesktop.Application.Windows.Options;
using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Settings;
using Google.Solutions.Settings.Collection;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Options
{
    [TestFixture]
    public class TestOptionsViewModelBase
    {
        public abstract class SettingsRepository : RepositoryBase<ISettingsCollection>
        {
            public SettingsRepository() : base(DictionarySettingsStore.Empty())
            {
            }
        }

        private class OptionsViewModel : OptionsViewModelBase<ISettingsCollection>
        {
            public bool ThrowOnSave = false;
            public int SaveCalls = 0;

            public OptionsViewModel(
                string title,
                RepositoryBase<ISettingsCollection> settingsRepository)
                : base(title, settingsRepository)
            {
                OnInitializationCompleted();
            }

            protected override void Save(ISettingsCollection settings)
            {
                this.SaveCalls++;
                Assert.IsNotNull(settings);

                if (this.ThrowOnSave)
                {
                    throw new ArgumentException("mock");
                }
            }

            public void MarkDirty()
            {
                this.IsDirty.Value = true;
            }

            public void CallMarkDirtyWhenPropertyChanges<T>(ObservableProperty<T> property)
            {
                MarkDirtyWhenPropertyChanges(property);
            }
        }

        private static Mock<SettingsRepository> CreateRepositoryMock()
        {
            var settings = new Mock<ISettingsCollection>();
            var repository = new Mock<SettingsRepository>();
            repository
                .Setup(r => r.GetSettings())
                .Returns(settings.Object);
            return repository;
        }

        //---------------------------------------------------------------------
        // Title.
        //---------------------------------------------------------------------

        [Test]
        public void Title()
        {
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                CreateRepositoryMock().Object);

            Assert.That(optionsViewModel.Title, Is.EqualTo("Sample"));
        }

        //---------------------------------------------------------------------
        // ApplyChanges.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyChanges_CallsSaveAndWritesBackSettings()
        {
            var repository = CreateRepositoryMock();
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                repository.Object);

            optionsViewModel.MarkDirty();
            optionsViewModel.ApplyChangesAsync().Wait();

            repository.Verify(r => r.GetSettings(), Times.Exactly(1));
            repository.Verify(r => r.SetSettings(It.IsAny<ISettingsCollection>()), Times.Once);
            Assert.That(optionsViewModel.SaveCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task ApplyChanges_WhenWriteBackSucceeds()
        {
            var repository = CreateRepositoryMock();
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                repository.Object);

            optionsViewModel.MarkDirty();

            await optionsViewModel.ApplyChangesAsync();
        }

        [Test]
        public void ApplyChanges_WhenSaveFails()
        {
            var repository = CreateRepositoryMock();
            repository
                .Setup(r => r.SetSettings(It.IsAny<ISettingsCollection>()))
                .Throws(new ArgumentException("mock"));
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                repository.Object)
            {
                ThrowOnSave = true
            };

            optionsViewModel.MarkDirty();

            Assert.Throws<ArgumentException>(() => optionsViewModel.ApplyChangesAsync().Wait());
        }

        [Test]
        public void ApplyChanges_WhenWriteBackFails()
        {
            var repository = CreateRepositoryMock();
            repository
                .Setup(r => r.SetSettings(It.IsAny<ISettingsCollection>()))
                .Throws(new ArgumentException("mock"));
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                repository.Object);

            optionsViewModel.MarkDirty();

            Assert.Throws<ArgumentException>(() => optionsViewModel.ApplyChangesAsync().Wait());
        }

        //---------------------------------------------------------------------
        // IsDirty.
        //---------------------------------------------------------------------

        [Test]
        public async Task ApplyChanges_ClearsDirtyFlag()
        {
            var repository = CreateRepositoryMock();
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                repository.Object);
            optionsViewModel.MarkDirty();

            Assert.IsTrue(optionsViewModel.IsDirty.Value);
            await optionsViewModel.ApplyChangesAsync();
            Assert.That(optionsViewModel.IsDirty.Value, Is.False);
        }

        [Test]
        public void IsDirty_WhenPropertyChanges()
        {
            var repository = CreateRepositoryMock();
            var optionsViewModel = new OptionsViewModel(
                "Sample",
                repository.Object);

            var property = ObservableProperty.Build(string.Empty);
            optionsViewModel.CallMarkDirtyWhenPropertyChanges(property);

            Assert.That(optionsViewModel.IsDirty.Value, Is.False);
            property.Value = "new value";
            Assert.IsTrue(optionsViewModel.IsDirty.Value);
        }
    }
}
