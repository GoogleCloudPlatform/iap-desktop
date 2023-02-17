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
        // Pubics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Add a rule to apply to every control of type TControl.
        /// Rules are applied in the order they're added.
        /// </summary>
        public void AddRule<TControl>(Action<TControl> apply)
            where TControl : Control
        {
            this.rules.AddLast(new Rule(
                typeof(TControl),
                c => apply((TControl)c)));
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
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private class Rule
        {
            private readonly Type controlType;
            private readonly Action<Control> apply;

            public Rule(Type controlType, Action<Control> apply)
            {
                this.controlType = controlType;
                this.apply = apply;
            }

            public void Apply(Control control)
            {
                if (this.controlType.IsAssignableFrom(control.GetType()))
                {
                    if (control.IsHandleCreated)
                    {
                        //
                        // Apply rule immediately.
                        //
                        this.apply(control);
                    }
                    else
                    {
                        //
                        // Delay until the handle has been created.
                        //
                        control.HandleCreated += OnHandleCreated;
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
