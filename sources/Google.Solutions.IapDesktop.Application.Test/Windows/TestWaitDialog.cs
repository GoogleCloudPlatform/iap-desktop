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

using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.Testing.Apis.Integration;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [RequiresInteraction]
    public class TestWaitDialog : ApplicationFixtureBase
    {
        //---------------------------------------------------------------------
        // Wait.
        //---------------------------------------------------------------------

        [Test]
        public void Wait_WhenTaskCompletedAlready()
        {
            WaitDialog.Wait(
                null,
                "Test...",
                _ => Task.CompletedTask);
        }

        [Test]
        public void Wait_WhenTaskCompletes()
        {
            WaitDialog.Wait(
                null,
                "Test...",
                _ => Task.Delay(1));
        }

        [Test]
        public void Wait_WhenTaskThrowsException_ThenWaitPropagatesException()
        {
            Assert.Throws<ArgumentException>(
                () => WaitDialog.Wait(
                    null,
                    "Test...",
                    _ => throw new ArgumentException()));
        }

        [Test]
        public void Wait_WhenTaskThrowsTaskCanceledException_ThenWaitPropagatesException()
        {
            Assert.Throws<TaskCanceledException>(
                () => WaitDialog.Wait(
                    null,
                    "Test...",
                    _ => throw new TaskCanceledException()));
        }

        [Test]
        public void Wait_WhenDialogCancelled_ThenWaitThrowsException()
        {
            Assert.Throws<TaskCanceledException>(
                () => WaitDialog.Wait(
                    null,
                    "Press cancel...",
                    async token =>
                    {
                        await Task.Delay(int.MaxValue, token);
                        return;
                    }));
        }
    }
}
