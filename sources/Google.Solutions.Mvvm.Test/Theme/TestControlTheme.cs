//
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

using Google.Solutions.Mvvm.Theme;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Theme
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestControlTheme
    {
        [Test]
        public void WhenControlIsNull_ThenApplyToReturns()
        {
            new ControlTheme().ApplyTo(null);
        }

        [Test]
        public void WhenControlContainsChildren_ThenApplyToAppliesRulesToAllChildren()
        {
            using (var form = new Form())
            {
                var panel = new Panel()
                {
                    Dock = DockStyle.Fill
                };
                form.Controls.Add(panel);

                var button = new Button();
                panel.Controls.Add(button);

                var theme = new ControlTheme();
                var appliedControls = new List<Control>();
                theme.AddRule<Control>(c => appliedControls.Add(c));
                
                theme.ApplyTo(form);

                form.Show();
                form.Close();
                
                CollectionAssert.AreEquivalent(
                    new Control[] { form, panel, button },
                    appliedControls);
            }
        }

        [Test]
        public void WhenRuleIsForSpecificType_ThenApplyToAppliesRuleToMatchingControls()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();

                int buttonsApplied = 0;
                theme.AddRule<Button>(c => buttonsApplied++);

                int controlsApplied = 0;
                theme.AddRule<Control>(c => controlsApplied++);

                form.Controls.Add(new Button());
                form.Controls.Add(new Panel());
                
                theme.ApplyTo(form);

                form.Show();
                form.Close();

                Assert.AreEqual(1, buttonsApplied);
                Assert.AreEqual(3, controlsApplied);
            }
        }

        [Test]
        public void WhenHandleNotCreatedYet_ThenRuleIsAppliedDelayed()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();

                int appliedCalls = 0;
                theme.AddRule<Form>(c => appliedCalls++);

                theme.ApplyTo(form);

                Assert.AreEqual(0, appliedCalls, "Call is delayed");

                form.Show();
                form.Close();

                Assert.AreEqual(1, appliedCalls, "Call is delayed");
            }
        }
    }
}
