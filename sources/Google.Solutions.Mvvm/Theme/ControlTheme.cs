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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Control theme that recursively applies rules based on the type of a control.
    /// </summary>
    public class ControlTheme : IControlTheme
    {
        private readonly LinkedList<Rule> rules = new LinkedList<Rule>();

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Add a rule to apply to every control of type TControl.
        /// Rules are applied in the order they're added.
        /// </summary>
        public void AddRule<TControl>(
            Action<TControl> apply,
            Options options = Options.None)
            where TControl : Control
        {
            this.rules.AddLast(new Rule(
                typeof(TControl),
                c => apply((TControl)c),
                options));
        }

        /// <summary>
        /// Add a set of rules.
        public ControlTheme AddRuleSet(IRuleSet ruleSet)
        {
            ruleSet.AddRules(this);
            return this;
        }

        //---------------------------------------------------------------------
        // IControlTheme.
        //---------------------------------------------------------------------

        public void ApplyTo(Control control)
        {
            if (control == null)
            {
                return;
            }

            //
            // Apply rules to this control.
            //
            foreach (var rule in this.rules)
            {
                rule.Apply(control);
            }

            //
            // Recursively visit child controls.
            //
            foreach (var child in control.Controls.OfType<Control>())
            {
                ApplyTo(child);
            }

            //
            // Apply to context menu strip. Context menu strips are
            // Controls too, but they're not included in the Controls list.
            //
            ApplyTo(control.ContextMenuStrip);

            if (control is ContainerControl || control is Panel)
            {
                //
                // Watch for new controls.
                //
                control.ControlAdded += (_, args) => 
                {
                    //
                    // Theming happens before a window is shown. Therefore,
                    // if we get a callback for an control that isn't
                    // visible yet, we can ignore this call.
                    //
                    // However, if the control is already visible,
                    // then that new child control must be the result
                    // of a programmatic control creation. For that,
                    // we need to apply the theme.
                    //
                    if (control.Visible)
                    {
                        ApplyTo(args.Control);
                    }
                };
            }
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        [Flags]
        public enum Options
        {
            None,
            ApplyWhenHandleCreated,
            IgnoreDerivedTypes
        }

        public interface IRuleSet
        {
            void AddRules(ControlTheme controlTheme);
        }

        private class Rule
        {
            private readonly Type controlType;
            private readonly Action<Control> apply;
            private readonly Options options;

            public Rule(
                Type controlType,
                Action<Control> apply,
                Options options)
            {
                this.controlType = controlType;
                this.apply = apply;
                this.options = options;
            }

            private bool Matches(Control control)
            {
                if (this.options.HasFlag(Options.IgnoreDerivedTypes))
                {
                    return this.controlType == control.GetType();
                }
                else
                {
                    return this.controlType.IsAssignableFrom(control.GetType());
                }
            }

            public void Apply(Control control)
            {
                if (Matches(control))
                {
                    if (!control.IsHandleCreated &&
                        this.options.HasFlag(Options.ApplyWhenHandleCreated))
                    {
                        //
                        // Delay until the handle has been created.
                        //
                        control.HandleCreated += OnHandleCreated;
                    }
                    else
                    {
                        //
                        // Apply rule immediately.
                        //
                        this.apply(control);
                    }

                    void OnHandleCreated(object _, EventArgs __)
                    {
                        Debug.Assert(control.IsHandleCreated);
                        this.apply(control);
                        control.HandleCreated -= OnHandleCreated;
                    }
                }
            }
        }
    }
}
