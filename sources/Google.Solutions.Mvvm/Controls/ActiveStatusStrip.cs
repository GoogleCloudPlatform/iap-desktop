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
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// A status strip that uses a different color based on
    /// its mode.
    /// </summary>
    public class ActiveStatusStrip : StatusStrip
    {
        private bool active = false;
        private Color activeBackColor = Control.DefaultBackColor;
        private Color activeForeColor = Control.DefaultForeColor;
        private Color inactiveBackColor = Control.DefaultBackColor;
        private Color inactiveForeColor = Control.DefaultForeColor;

        public EventHandler ActiveChanged;

        private void ApplyColors()
        {
            this.ForeColor = this.active ? this.ActiveForeColor : this.InactiveForeColor;
            this.BackColor = this.active ? this.InactiveBackColor : this.ActiveBackColor;
        }

        //---------------------------------------------------------------------
        // Color properties.
        //---------------------------------------------------------------------

        /// <summary>
        /// Background color in active mode.
        /// </summary>
        public Color ActiveBackColor
        {
            get => this.activeBackColor;
            set
            {
                this.activeBackColor = value;
                ApplyColors();
            }
        }

        /// <summary>
        /// Foreground color in active mode.
        /// </summary>
        public Color ActiveForeColor
        {
            get => this.activeForeColor;
            set
            {
                this.activeForeColor = value;
                ApplyColors();
            }
        }

        /// <summary>
        /// Background color in inactive mode.
        /// </summary>
        public Color InactiveBackColor
        {
            get => this.inactiveBackColor;
            set
            {
                this.inactiveBackColor = value;
                ApplyColors();
            }
        }

        /// <summary>
        /// Foreground color in inactive mode.
        /// </summary>
        public Color InactiveForeColor
        {
            get => this.inactiveForeColor;
            set
            {
                this.inactiveForeColor = value;
                ApplyColors();
            }
        }

        public bool Active 
        {
            get => this.active;
            set
            {
                this.active = value;
                ApplyColors();
                this.ActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
