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
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestListViewExtensions
    {
        private ListView listView;
        private Form form;

        [SetUp]
        public void SetUp()
        {
            this.listView = new ListView();
            this.listView.Columns.Add(new ColumnHeader()
            {
                Text = "The \"first\" header",
                DisplayIndex = 0
            });
            this.listView.Columns.Add(new ColumnHeader()
            {
                Text = "The second header",
                DisplayIndex = 0
            });
            this.listView.Items.Add(new ListViewItem(new[] {
                "\"first\" item",
                "-42"
            }));

            this.listView.View = View.Details;

            this.form = new Form();
            this.form.Controls.Add(this.listView);

            this.form.Show();
        }

        [TearDown]
        public void TearDown()
        {
            this.form.Close();
        }

        //---------------------------------------------------------------------
        // ToXxx.
        //---------------------------------------------------------------------

        [Test]
        public void ToTabSeparatedText_WhenListPopulated_ThenToTabSeparatedTextSucceeds()
        {
            var tsv = this.listView.ToTabSeparatedText(false);

            Assert.AreEqual(
                "\"The 'first' header\"\t\"The second header\"\r\n\"" +
                "'first' item\"\t\"-42\"\r\n",
                tsv);
        }

        [Test]
        public void ToHtml_WhenListPopulated_ThenToHtmlSucceeds()
        {
            var html = this.listView.ToHtml(false);

            Assert.AreEqual(
                "<table>\r\n" +
                "<tr>\r\n" +
                "<th>The &quot;first&quot; header</th><th>The second header</th>\r\n" +
                "</tr>\r\n" +
                "<tr>\r\n" +
                "<td>&quot;first&quot; item</td><td>-42</td>\r\n" +
                "</tr>\r\n" +
                "</table>\r\n",
                html);
        }
    }
}
