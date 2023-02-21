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

using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    internal class VSThemeExtensions
    {
        internal class ToolStripRenderer : VisualStudioToolStripRenderer
        {
            private readonly DockPanelColorPalette palette;

            public ToolStripRenderer(DockPanelColorPalette palette) : base(palette)
            {
                this.palette = palette;
                base.UseGlassOnMenuStrip = false;
            }

            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                if (e.Item is ToolStripMenuItem item && item != null)
                {
                    //
                    // The base class doesn't adjust the arrow color. That's
                    // okay in light mode, but makes arrows almost invisible
                    // in dark mode.
                    //
                    // Apply color from theme.
                    //
                    e.ArrowColor = this.palette.CommandBarMenuPopupDefault.Arrow;
                }

                base.OnRenderArrow(e);
            }
        }

        // TODO: Move other extenders here
    }
}
