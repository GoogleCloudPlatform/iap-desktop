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

using Google.Solutions.Common.Util;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Theme
{
    /// <summary>
    /// Theming rules for ensuring compatibility with GDI scaling.
    /// </summary>
    public class GdiScalingRuleset : ControlTheme.IRuleSet
    {
        private readonly PropertyInfo doubleBufferedProperty;

        public GdiScalingRuleset()
        {
            this.doubleBufferedProperty = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        //---------------------------------------------------------------------
        // Theming rules.
        //---------------------------------------------------------------------

        private void DisableDoubleBuffering<TControl>(TControl control)
            where TControl : Control
        {
            //
            // GDI scaling doesn't work if a control uses double-buffering.
            // For simple controls, it's a reasonable trade-off to sacrifice
            // double-buffering in exchange for crisp text.
            //
            if (GdiScaling.IsEnabled && this.doubleBufferedProperty != null)
            {
                this.doubleBufferedProperty.SetValue(control, false);
            }
        }

        private void DisableDoubleBufferingForLabel(Label label)
        {
            if (label is LinkLabel) 
            {
                //
                // Disabling double-buffering causes the control to be
                // cropped, so leave it on.
                //
            }
            else
            {
                DisableDoubleBuffering(label);
            }
        }

        //---------------------------------------------------------------------
        // IRuleSet
        //---------------------------------------------------------------------

        /// <summary>
        /// Register rules.
        /// </summary>
        public void AddRules(ControlTheme controlTheme)
        {
            controlTheme.ExpectNotNull(nameof(controlTheme));

            controlTheme.AddRule<Label>(DisableDoubleBufferingForLabel);
            controlTheme.AddRule<CheckBox>(DisableDoubleBuffering);
            controlTheme.AddRule<Button>(DisableDoubleBuffering);
            controlTheme.AddRule<ToolStrip>(DisableDoubleBuffering);
            controlTheme.AddRule<ContextMenuStrip>(DisableDoubleBuffering);
        }
    }
}
