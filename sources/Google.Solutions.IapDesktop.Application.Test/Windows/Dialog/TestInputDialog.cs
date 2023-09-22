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

using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using Google.Solutions.Testing.Apis.Integration;
using Moq;
using NUnit.Framework;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows.Dialog
{
    [TestFixture]
    public class TestInputDialog
    {
        //---------------------------------------------------------------------
        // Prompt.
        //---------------------------------------------------------------------

        [InteractiveTest]
        [Test]
        public void Prompt()
        {
            void validate(
                string value,
                out bool valid,
                out string warning)
            {
                valid = int.TryParse(value, out var _);
                warning = valid
                    ? null
                    : "This is not a number";
            }

            var dialog = new InputDialog(
                new Service<IThemeService>(new Mock<IServiceProvider>().Object));

            if (dialog.Prompt(
                null,
                new InputDialogParameters()
                {
                    Title = "This is the title",
                    Caption = "This is the caption",
                    Message = "Enter a number",
                    Validate = validate
                },
                out var input) == DialogResult.OK)
            {
                Assert.NotNull(input);
                Assert.IsTrue(int.TryParse(input, out var _));
            }
        }
    }
}
