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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace Google.Solutions.Terminal.Controls
{
    public partial class VirtualTerminal
    {
        private Color selectionBackColor = SystemColors.HighlightText;
        private float selectionBackgroundAlpha = .5f;
        private CaretStyle caretStyle = CaretStyle.BlinkingBlockDefault;
        private Font font = new Font(new FontFamily(DefaultFontFamily), DefaultFontSize);
        private TerminalColors terminalColors = TerminalColors.Default;

        [Category("Appearance")]
        public Color SelectionBackColor
        {
            get => this.selectionBackColor;
            set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                this.selectionBackColor = value;
                OnThemeChanged();
            }
        }

        [Category("Appearance")]
        public float SelectionBackgroundAlpha
        {
            get => this.selectionBackgroundAlpha;
            set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this.selectionBackgroundAlpha = value;
                OnThemeChanged();
            }
        }

        [Category("Appearance")]
        public CaretStyle Caret
        {
            get => this.caretStyle;
            set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                this.caretStyle = value;
                OnThemeChanged();
            }
        }

        [Category("Appearance")]
        public TerminalColors TerminalColors
        {
            get => this.terminalColors;
            set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                this.terminalColors = value;
                OnThemeChanged();
            }
        }

        [Category("Appearance")]
        public new Font Font
        {
            //
            // NB. Control.Font has side-effects, so we don't use that.
            //
            get => this.font;
            set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                this.font = value;
                OnThemeChanged();
            }
        }

        /// <summary>
        /// Enable Ctrl+Insert to copy.
        /// </summary>
        [Category("Behavior")]
        public bool EnableCtrlInsert { get; set; } = true;

        /// <summary>
        /// Enable Shift+Insert to paste.
        /// </summary>
        [Category("Behavior")]
        public bool EnableShiftInsert { get; set; } = true;

        /// <summary>
        /// Enable Ctrl+C to copy.
        /// </summary>
        [Category("Behavior")]
        public bool EnableCtrlC { get; set; } = true;

        /// <summary>
        /// Enable Ctrl+V to paste.
        /// </summary>
        [Category("Behavior")]
        public bool EnableCtrlV { get; set; } = true;
    }
}
