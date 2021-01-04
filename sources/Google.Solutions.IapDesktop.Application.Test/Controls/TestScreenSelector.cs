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

using Google.Solutions.IapDesktop.Application.Controls;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestScreenSelector : ApplicationFixtureBase
    {
        private Form form;

        private class ScreenSelectorItem : IScreenSelectorModelItem
        {
            public Screen Screen { get; }

            public bool IsSelected { get; set; }

            public ScreenSelectorItem(Screen screen)
            {
                this.Screen = screen;
                this.IsSelected = false;
            }
        }

        [Test]
        public void __()
        {
            var model = new ObservableCollection<ScreenSelectorItem>();
            foreach (var s in Screen.AllScreens)
            {
                model.Add(new ScreenSelectorItem(s));
            }

            var selector = new ScreenSelector<ScreenSelectorItem>()
            {
                Dock = DockStyle.Fill
            };

            selector.BindCollection(model);
            this.form = new Form();
            this.form.Controls.Add(selector);

            this.form.ShowDialog();
        }
    }
}
