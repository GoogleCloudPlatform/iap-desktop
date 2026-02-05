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
            new ControlTheme().ApplyTo(null!);
        }

        //---------------------------------------------------------------------
        // Control nesting.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyTo_WhenControlContainsChildren_ThenApplyToAppliesRulesToAllChildren()
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

                Assert.That(
                    appliedControls, Is.EquivalentTo(new Control[] { form, panel, button }));
            }
        }

        [Test]
        public void ApplyTo_WhenControlHasContextMenu_ThenApplyToAppliesRulesToContextMenu()
        {
            using (var form = new Form()
            {
                ContextMenuStrip = new ContextMenuStrip()
            })
            {
                var theme = new ControlTheme();
                var appliedControls = new List<Control>();
                theme.AddRule<Control>(c => appliedControls.Add(c));

                theme.ApplyTo(form);

                form.Show();
                form.Close();

                Assert.That(
                    appliedControls, Is.EquivalentTo(new Control[] { form, form.ContextMenuStrip }));
            }
        }

        [Test]
        public void ApplyTo_WhenControlAddedLater()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();
                var appliedControls = new List<Control>();
                theme.AddRule<Control>(c => appliedControls.Add(c));
                theme.ApplyTo(form);

                var textBox = new TextBox();
                form.Controls.Add(textBox);

                form.Show();

                var label = new Label();
                form.Controls.Add(label);

                form.Close();

                Assert.That(
                    appliedControls, Is.EquivalentTo(new Control[] { form, label }),
                    "Label themed after show, textbox is ignored");
            }
        }

        //---------------------------------------------------------------------
        // Rule matching.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyTo_WhenNoOptionSet_ThenRuleAppliesToDerivedControls()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();

                var buttonsApplied = 0;
                theme.AddRule<Button>(c => buttonsApplied++);

                var controlsApplied = 0;
                theme.AddRule<Control>(c => controlsApplied++);

                form.Controls.Add(new Button());
                form.Controls.Add(new Panel());

                theme.ApplyTo(form);

                form.Show();
                form.Close();

                Assert.That(buttonsApplied, Is.EqualTo(1));
                Assert.That(controlsApplied, Is.EqualTo(3));
            }
        }

        [Test]
        public void ApplyTo_WhenIgnoreDerivedTypesOptionSet_ThenRuleIgnoresDerivedControls()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();

                var buttonsApplied = 0;
                theme.AddRule<Button>(
                    c => buttonsApplied++,
                    ControlTheme.Options.IgnoreDerivedTypes);

                var controlsApplied = 0;
                theme.AddRule<Control>(
                    c => controlsApplied++,
                    ControlTheme.Options.IgnoreDerivedTypes);

                form.Controls.Add(new Button());
                form.Controls.Add(new Panel());

                theme.ApplyTo(form);

                form.Show();
                form.Close();

                Assert.That(buttonsApplied, Is.EqualTo(1));
                Assert.That(controlsApplied, Is.EqualTo(0));
            }
        }

        //---------------------------------------------------------------------
        // Defer till handle created.
        //---------------------------------------------------------------------

        [Test]
        public void ApplyTo_WhenNoOptionSetAndHandleNotCreatedYet_ThenRuleIsAppliedImmediately()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();

                var appliedCalls = 0;
                theme.AddRule<Form>(c => appliedCalls++);

                theme.ApplyTo(form);

                Assert.That(appliedCalls, Is.EqualTo(1), "Call is delayed");
            }
        }

        [Test]
        public void ApplyTo_WhenDelayOptionSetAndHandleNotCreatedYet_ThenRuleIsAppliedDelayed()
        {
            using (var form = new Form())
            {
                var theme = new ControlTheme();

                var appliedCalls = 0;
                theme.AddRule<Form>(c => appliedCalls++, ControlTheme.Options.ApplyWhenHandleCreated);

                theme.ApplyTo(form);

                Assert.That(appliedCalls, Is.EqualTo(0), "Call is delayed");

                form.Show();
                form.Close();

                Assert.That(appliedCalls, Is.EqualTo(1), "Call is delayed");
            }
        }
    }
}
