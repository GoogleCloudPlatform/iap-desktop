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

using Google.Solutions.Mvvm.Format;
using Google.Solutions.Testing.Common.Integration;
using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public partial class TestMarkdownViewer : Form
    {
        public TestMarkdownViewer()
        {
            InitializeComponent();

            this.markdown.Markdown = this.sourceText.Text;
            this.rtf.Text = MarkdownDocument.Parse(this.sourceText.Text).ToRtf();

            this.sourceText.TextChanged += (_, __) =>
            {
                this.markdown.Markdown = this.sourceText.Text;
                this.rtf.Text = MarkdownDocument.Parse(this.sourceText.Text).ToRtf();
            };
        }

        [InteractiveTest]
        [Test]
        public void ConvertToMarkdown()
        {
            ShowDialog();
        }
    }
}
