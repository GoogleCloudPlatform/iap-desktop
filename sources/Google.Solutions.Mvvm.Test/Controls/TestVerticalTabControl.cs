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
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestVerticalTabControl
    {
        [RequiresInteraction]
        [Test]
        public void TestUI()
        {
            using (var form = new Form()
            {
                Width = 800,
                Height = 600,
            })
            {
                var tabControl = new VerticalTabControl()
                {
                    BackColor = Color.Yellow,
                    Dock = DockStyle.Fill,
                    ActiveTabForeColor = Color.Yellow,
                    ActiveTabBackColor = Color.Red,
                    InactiveTabBackColor = SystemColors.ControlDarkDark,
                    HoverTabBackColor = Color.LightBlue,
                    HoverTabForeColor = Color.DarkBlue
                };
                tabControl.TabPages.Add(new TabPage()
                {
                    Text = "One",
                    BackColor = Color.AliceBlue
                });
                tabControl.TabPages.Add(new TabPage()
                {
                    Text = "Two",
                    BackColor = Color.AliceBlue
                });
                tabControl.TabPages.Add(new TabPage()
                {
                    Text = "This one has a very, very, very, very, very, long caption"
                });
                form.Controls.Add(tabControl);

                form.ShowDialog();
            }
        }
    }
}
