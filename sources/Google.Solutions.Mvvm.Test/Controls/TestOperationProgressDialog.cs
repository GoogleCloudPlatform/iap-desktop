//
// Copyright 2024 Google LLC
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
    public class TestOperationProgressDialog
    {
        [Test]
        public void StartCopyOperation()
        {
            using (var form = new Form())
            {
                form.Show();
                using (var op = new OperationProgressDialog()
                    .StartCopyOperation(form, 1, 2))
                {
                    op.OnBytesCompleted(1);
                    op.OnBytesCompleted(2);
                    op.OnItemCompleted();
                }
            }
        }

        [Test]
        public void IsBlockedByError()
        {
            using (var form = new Form())
            {
                form.Show();
                using (var op = new OperationProgressDialog()
                    .StartCopyOperation(form, 1, 2))
                {
                    Assert.IsFalse(op.IsBlockedByError);

                    op.IsBlockedByError = true;
                    op.IsBlockedByError = true;

                    Assert.IsTrue(op.IsBlockedByError);

                    op.IsBlockedByError = false;
                }
            }
        }
    }
}
