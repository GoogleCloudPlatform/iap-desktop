﻿//
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

using Google.Solutions.Mvvm.Theme;
using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestCommonControlTheme
    {
        [Test]
        public void WhenListWiderThanColumns_ThenLastColumnIsEnlarged()
        {
            using (var form = new Form()
            {
                Width = 400,
                Height = 400
            })
            {
                var listView = new ListView()
                {
                    Dock = DockStyle.Fill,
                    View = View.Details
                };
                var column1 = new ColumnHeader()
                {
                    Text = "Col-1",
                    Width = 100
                };
                var column2 = new ColumnHeader()
                {
                    Text = "Col-2",
                    Width = 100
                };

                listView.Columns.Add(column1);
                listView.Columns.Add(column2);

                form.Controls.Add(listView);

                new CommonControlRuleSet().AutoSizeListViewColumns(listView);
                form.Show();

                Assert.AreEqual(100, column1.Width);
                Assert.AreNotEqual(100, column2.Width);

                form.Close();
            }
        }

        [Test]
        public void WhenColumnResized_ThenLastColumnIsResizedToFit()
        {
            using (var form = new Form()
            {
                Width = 400,
                Height = 400
            })
            {
                var listView = new ListView()
                {
                    Dock = DockStyle.Fill,
                    View = View.Details
                };
                var column1 = new ColumnHeader()
                {
                    Text = "Col-1",
                    Width = 200
                };
                var column2 = new ColumnHeader()
                {
                    Text = "Col-2",
                    Width = 200
                };

                listView.Columns.Add(column1);
                listView.Columns.Add(column2);

                form.Controls.Add(listView);

                new CommonControlRuleSet().AutoSizeListViewColumns(listView);
                form.Show();

                column1.Width = 50;

                Assert.AreEqual(50, column1.Width);
                Assert.AreNotEqual(200, column2.Width);

                form.Close();
            }
        }

        [Test]
        public void WhenListNarrowerThanColumns_ThenLastColumnIsShrunk()
        {
            using (var form = new Form()
            {
                Width = 400,
                Height = 400
            })
            {
                var listView = new ListView()
                {
                    Dock = DockStyle.Fill,
                    View = View.Details
                };
                var column1 = new ColumnHeader()
                {
                    Text = "Col-1",
                    Width = 100
                };
                var column2 = new ColumnHeader()
                {
                    Text = "Col-2",
                    Width = 500
                };

                listView.Columns.Add(column1);
                listView.Columns.Add(column2);

                form.Controls.Add(listView);

                new CommonControlRuleSet().AutoSizeListViewColumns(listView);
                form.Show();

                Assert.AreEqual(100, column1.Width);
                Assert.AreNotEqual(100, column2.Width);

                form.Close();
            }
        }
    }
}
