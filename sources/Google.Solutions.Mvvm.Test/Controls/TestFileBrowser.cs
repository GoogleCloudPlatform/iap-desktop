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

using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Testing.Common;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.Mvvm.Controls.FileBrowser;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestFileBrowser
    {
        private static readonly FileType SampleFileType 
            = new FileType("Sample", false, SystemIcons.Application.ToBitmap());

        //---------------------------------------------------------------------
        // Bind.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBoundAlready_ThenBindThrowsException()
        {
            var root = new Mock<IFileItem>();
            root.SetupGet(i => i.Name).Returns("Item");
            root.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            root.SetupGet(i => i.Type).Returns(SampleFileType);
            root.SetupGet(i => i.Size).Returns(1);
            root.SetupGet(i => i.IsExpanded).Returns(true);

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                browser.Bind(
                    root.Object,
                    i => Task.FromResult(new ObservableCollection<IFileItem>())));

                Assert.Throws<InvalidOperationException>(
                    () => browser.Bind(
                        root.Object,
                        i => Task.FromResult(new ObservableCollection<IFileItem>())));
            }
        }

        //---------------------------------------------------------------------
        // NavigationFailed.
        //---------------------------------------------------------------------

        [Test]
        public void WhenListingFilesFails_ThenNavigationFailedEventIsRaised()
        {
            var root = new Mock<IFileItem>();
            root.SetupGet(i => i.Name).Returns("Item");
            root.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            root.SetupGet(i => i.Type).Returns(SampleFileType);
            root.SetupGet(i => i.Size).Returns(1);
            root.SetupGet(i => i.IsExpanded).Returns(true);

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                bool eventRaised = false;
                browser.NavigationFailed += (sender, args) =>
                {
                    Assert.AreSame(browser, sender);
                    Assert.IsInstanceOf<ApplicationException>(args.Exception.Unwrap());
                    eventRaised = true;
                };

                browser.Bind(
                    root.Object,
                    i => Task.FromException<ObservableCollection<IFileItem>>(new ApplicationException("test")));

                Application.DoEvents();
                Assert.IsTrue(eventRaised);
            }
        }

        //---------------------------------------------------------------------
        // CurrentFolder.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBound_ThenCurrentFolderSetToRoot()
        {
            var root = new Mock<IFileItem>();
            root.SetupGet(i => i.Name).Returns("Item");
            root.SetupGet(i => i.LastModified).Returns(DateTime.UtcNow);
            root.SetupGet(i => i.Type).Returns(SampleFileType);
            root.SetupGet(i => i.Size).Returns(1);
            root.SetupGet(i => i.IsExpanded).Returns(true);

            using (var form = new Form()
            {
                Size = new Size(800, 600)
            })
            {
                var browser = new FileBrowser()
                {
                    Dock = DockStyle.Fill
                };

                bool eventRaised = false;
                browser.CurrentFolderChanged += (sender, args) =>
                {
                    eventRaised = true;
                };

                browser.Bind(
                    root.Object,
                    i => Task.FromResult(new ObservableCollection<IFileItem>()));

                Application.DoEvents();

                Assert.AreSame(root.Object, browser.CurrentFolder);
                Assert.IsTrue(eventRaised);
            }
        }
    }
}
