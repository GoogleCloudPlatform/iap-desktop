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

using Google.Solutions.Mvvm.Controls;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestScreenPicker
    {
        private class ScreenSelectorItem : IScreenPickerModelItem
        {
            public Rectangle ScreenBounds { get; set; }

            public string DeviceName { get; set; }

            public bool IsSelected { get; set; }

            public ScreenSelectorItem(string deviceName)
            {
                this.DeviceName = deviceName;
            }
        }

        [Test]
        public void WhenNoModelProvided_ThenShowDialogSucceeds()
        {
            var picker = new ScreenPicker<ScreenSelectorItem>()
            {
                Dock = DockStyle.Fill
            };

            using (var form = new Form())
            {
                form.Controls.Add(picker);
                form.Show();
                System.Windows.Forms.Application.DoEvents();
            }
        }

        [Test]
        public void ShowDialog_WhenModelHasScreensWithNegativeBounds_ThenShowDialogSucceeds()
        {
            var model = new ObservableCollection<ScreenSelectorItem>
            {
                new ScreenSelectorItem("first")
                {
                    ScreenBounds = new Rectangle()
                    {
                        X = -1000,
                        Y = -1000,
                        Width = 500,
                        Height = 500
                    }
                },

                new ScreenSelectorItem("second")
                {
                    ScreenBounds = new Rectangle()
                    {
                        X = 500,
                        Y = 500,
                        Width = 500,
                        Height = 500
                    }
                }
            };

            var picker = new ScreenPicker<ScreenSelectorItem>()
            {
                Dock = DockStyle.Fill
            };

            picker.BindCollection(model);

            using (var form = new Form()
            {
                Width = 400,
                Height = 400
            })
            {
                form.Controls.Add(picker);

                form.Show();
                System.Windows.Forms.Application.DoEvents();

                Assert.That(picker.Screens.Count(), Is.EqualTo(2));
                Assert.That(picker.Screens.First().Bounds.X, Is.EqualTo(2));
                Assert.That(picker.Screens.First().Bounds.Y, Is.EqualTo(2));

                Assert.That(picker.Screens.Last().Bounds.Y + picker.Screens.Last().Bounds.Height, Is.EqualTo(form.ClientSize.Height - 2).Within(2));
            }
        }
    }
}
