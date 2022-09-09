﻿//
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

using Google.Solutions.Mvvm.Commands;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Commands
{
    [TestFixture]
    public class TestCommand
    {
        //---------------------------------------------------------------------
        // ActivityText.
        //---------------------------------------------------------------------

        [Test]
        public void WhenActivityTextNotSet_ThenActivityTextReturnsText()
        {
            var command = new Command<string>(
                "&Sample",
                _ => CommandState.Enabled,
                _ => { });

            Assert.AreEqual("Sample", command.ActivityText);
        }
        [Test]
        public void WhenActivityTextSet_ThenActivityTextReturnsActivityText()
        {
            var command = new Command<string>(
                "&Sample",
                _ => CommandState.Enabled,
                _ => { })
            {
                ActivityText = "Doing tests"
            };

            Assert.AreEqual("Doing tests", command.ActivityText);
        }
    }
}
