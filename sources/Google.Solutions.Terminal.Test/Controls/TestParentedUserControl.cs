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

using Google.Solutions.Terminal.Controls;
using Google.Solutions.Testing.Apis;
using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestParentedUserControl
    {
        private class SampleForm : ParentedUserControl
        {
            public int ParentFormChanges { get; private set; } = 0;
            public new Form? CurrentParentForm => base.CurrentParentForm;

            protected override void OnCurrentParentFormChanged()
            {
                this.ParentFormChanges++;
            }
        }

        [WindowsFormsTest]
        public void CurrentParentForm_WhenNotParented()
        {
            var control = new SampleForm();
            Assert.IsNull(control.CurrentParentForm);
        }

        [WindowsFormsTest]
        public void CurrentParentForm_WhenParented()
        {
            using (var form = new Form())
            {
                var control = new SampleForm();
                form.Controls.Add(control);

                form.Show();
                Application.DoEvents();

                Assert.IsNotNull(control.CurrentParentForm);
                Assert.AreEqual(1, control.ParentFormChanges);
            }
        }

        [WindowsFormsTest]
        public void CurrentParentForm_WhenReparented()
        {
            using (var form = new Form())
            {
                var control = new SampleForm();
                form.Controls.Add(control);

                form.Show();
                Application.DoEvents();

                Assert.AreSame(form, control.CurrentParentForm);
                Assert.AreEqual(1, control.ParentFormChanges);

                using (var newForm = new Form())
                {
                    form.Controls.Clear();
                    newForm.Controls.Add(control);

                    Assert.AreSame(newForm, control.CurrentParentForm);
                    Assert.AreEqual(2, control.ParentFormChanges);
                }
            }
        }
    }
}
