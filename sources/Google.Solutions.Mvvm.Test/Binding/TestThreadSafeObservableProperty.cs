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

using Google.Solutions.Mvvm.Binding;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [Apartment(ApartmentState.STA)]
    [TestFixture]
    public class TestThreadSafeObservableProperty
    {
        private class SampleViewModel : ViewModelBase
        {
        }

        [Test]
        public void Value_InitialValue()
        {
            using (var viewModel = new SampleViewModel())
            using (var form = new Form())
            {
                viewModel.View = form;

                var property = ObservableProperty.Build("initial", viewModel);

                Assert.AreEqual("initial", property.Value);
            }
        }

        [Test]
        public async Task RaisePropertyChange()
        {
            using (var viewModel = new SampleViewModel())
            using (var form = new Form())
            {
                viewModel.View = form;

                var property = ObservableProperty.Build(string.Empty, viewModel);

                var eventRaised = false;
                property.PropertyChanged += (_, __) =>
                {
                    Assert.IsFalse(form.InvokeRequired);
                    eventRaised = true;
                };

                await Task.Factory
                    .StartNew(() => property.RaisePropertyChange())
                    .ConfigureAwait(true);

                Assert.IsTrue(eventRaised);
            }
        }

        [Test]
        public async Task RaisePropertyChange_NotifiesDependents()
        {
            using (var viewModel = new SampleViewModel())
            using (var form = new Form())
            {
                viewModel.View = form;

                var property = ObservableProperty.Build(string.Empty, viewModel);
                var dependent1 = ObservableProperty.Build(
                    property,
                    s => s.ToUpper());

                var eventRaised = false;
                dependent1.PropertyChanged += (_, __) =>
                {
                    Assert.IsFalse(form.InvokeRequired);
                    eventRaised = true;
                };

                await Task.Factory
                    .StartNew(() => property.RaisePropertyChange())
                    .ConfigureAwait(true);

                Assert.IsTrue(eventRaised);
            }
        }
    }
}
