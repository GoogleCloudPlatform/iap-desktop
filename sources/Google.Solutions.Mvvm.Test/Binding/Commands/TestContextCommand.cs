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

using Google.Solutions.Mvvm.Binding.Commands;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Binding.Commands
{
    [TestFixture]
    public class TestContextCommand
    {
        //---------------------------------------------------------------------
        // ActivityText.
        //---------------------------------------------------------------------

        [Test]
        public void ActivityText_WhenActivityTextNotSet_ThenActivityTextReturnsText()
        {
            var command = new ContextCommand<string>(
                "&Sample",
                _ => CommandState.Enabled,
                _ => { });

            Assert.That(command.ActivityText, Is.EqualTo("Sample"));
        }
        [Test]
        public void ActivityText_WhenActivityTextSet_ThenActivityTextReturnsActivityText()
        {
            var command = new ContextCommand<string>(
                "&Sample",
                _ => CommandState.Enabled,
                _ => { })
            {
                ActivityText = "Doing tests"
            };

            Assert.That(command.ActivityText, Is.EqualTo("Doing tests"));
        }
    }
}
