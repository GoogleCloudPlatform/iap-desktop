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

using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Testing.Common.Integration;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestProgressBar
    {

        //---------------------------------------------------------------------
        // Indeterminate.
        //---------------------------------------------------------------------

        [Test]
        public void Indeterminate()
        {
            using (var progressBar = new LinearProgressBar())
            {
                Assert.IsFalse(progressBar.Indeterminate);
                Assert.IsNull(progressBar.timer);

                progressBar.Indeterminate = true;
                progressBar.Indeterminate = true;
                Assert.IsTrue(progressBar.Indeterminate);
                Assert.IsNotNull(progressBar.timer);

                progressBar.Indeterminate = false;
                progressBar.Indeterminate = false;
                Assert.IsFalse(progressBar.Indeterminate);
                Assert.IsNull(progressBar.timer);
            }
        }

        [Test]
        public void Value()
        {
            using (var progressBar = new LinearProgressBar())
            {
                Assert.AreEqual(0, progressBar.Value);

                progressBar.Value = progressBar.Maximum;
                Assert.AreEqual(progressBar.Maximum, progressBar.Maximum);

                progressBar.Maximum = progressBar.Maximum + 1;
                Assert.AreEqual(progressBar.Maximum, progressBar.Maximum);
            }
        }

        [Test]
        public void Maximum()
        {
            using (var progressBar = new LinearProgressBar())
            {
                progressBar.Maximum = 0;
                Assert.AreEqual(1, progressBar.Maximum);

                progressBar.Maximum = -10;
                Assert.AreEqual(1, progressBar.Maximum);
            }
        }

        [Test]
        public void Speed()
        {
            using (var progressBar = new LinearProgressBar())
            {
                Assert.Throws<ArgumentException>(() => progressBar.Speed = 0);
                Assert.Throws<ArgumentException>(() => progressBar.Speed = -1);

                progressBar.Speed = progressBar.Maximum + 1;
                Assert.AreEqual(progressBar.Maximum, progressBar.Maximum);
            }
        }

        //---------------------------------------------------------------------
        // CircularProgressBar.
        //---------------------------------------------------------------------

        [InteractiveTest]
        [Test]
        public void DefiniteCircularProgressBar()
        {
            using (var form = new Form()
            {
                Width = 200,
                Height = 200,
                BackColor = Color.DarkGray
            })
            using (var progressBar = new CircularProgressBar()
            {
                ForeColor = Color.Yellow,
                Value = 1,
                Maximum = 10,
                Dock = DockStyle.Fill
            })
            {
                progressBar.Click += (_, __) 
                    => progressBar.Value = (progressBar.Value + 1) % progressBar.Maximum;
                form.Controls.Add(progressBar);
                form.ShowDialog();
            }
        }

        [InteractiveTest]
        [Test]
        public void IndeterminateCircularProgressBar()
        {
            using (var form = new Form()
            {
                Width = 200,
                Height = 200,
                BackColor = Color.DarkGray
            })
            using (var progressBar = new CircularProgressBar()
            {
                ForeColor = Color.Yellow,
                Indeterminate = true,
                Speed = 1,
                Dock = DockStyle.Fill
            })
            {
                form.Controls.Add(progressBar);
                form.ShowDialog();
            }
        }

        //---------------------------------------------------------------------
        // LinearProgressBar.
        //---------------------------------------------------------------------

        [InteractiveTest]
        [Test]
        public void DefiniteLinearProgressBar()
        {
            using (var form = new Form()
            {
                Width = 300,
                Height = 80,
                BackColor = Color.DarkGray
            })
            using (var progressBar = new LinearProgressBar()
            {
                ForeColor = Color.Yellow,
                Value = 7,
                Maximum = 10,
                Dock = DockStyle.Fill
            })
            {
                progressBar.Click += (_, __)
                    => progressBar.Value = (progressBar.Value + 1) % (progressBar.Maximum + 1);
                form.Controls.Add(progressBar);
                form.ShowDialog();
            }
        }

        [InteractiveTest]
        [Test]
        public void IndeterminateLinearProgressBar()
        {
            using (var form = new Form()
            {
                Width = 300,
                Height = 80,
                BackColor = Color.DarkGray
            })
            using (var progressBar = new LinearProgressBar()
            {
                ForeColor = Color.Yellow,
                Indeterminate = true,
                Dock = DockStyle.Fill
            })
            {
                form.Controls.Add(progressBar);
                form.ShowDialog();
            }
        }
    }
}
