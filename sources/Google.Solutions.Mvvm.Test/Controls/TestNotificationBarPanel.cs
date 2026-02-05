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

using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;
namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public partial class TestNotificationBarPanel : Form
    {
        public TestNotificationBarPanel()
        {
            InitializeComponent();

            this.textBox.TextChanged += (_, __)
                => this.notificationBarPanel.Text = this.textBox.Text;
        }

        //---------------------------------------------------------------------
        // UI tests.
        //---------------------------------------------------------------------

        [RequiresInteraction]
        [Test]
        public void ShowTestUi()
        {
            ShowDialog();
        }

        //---------------------------------------------------------------------
        // Unit tests.
        //---------------------------------------------------------------------

        [Test]
        public void WhenTextNullOrWhiteSpace_ThenBarIsHidden(
            [Values("", " ", null)] string? emptyValue)
        {
            using (var form = new Form()
            {
                Width = 400,
                Height = 400
            })
            {
                var panel = new NotificationBarPanel();
                form.Controls.Add(panel);

                form.Show();

                Assert.That(panel.NotificationBarVisible, Is.False);
                panel.Text = "Note!";
                Assert.IsTrue(panel.NotificationBarVisible);
                panel.Text = emptyValue;
                Assert.That(panel.NotificationBarVisible, Is.False);

                form.Close();
            }
        }
    }
}
