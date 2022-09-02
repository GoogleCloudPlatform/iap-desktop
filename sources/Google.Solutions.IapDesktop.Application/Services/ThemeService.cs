//
// Copyright 2020 Google LLC
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

using Google.Solutions.IapDesktop.Application.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using static WeifenLuo.WinFormsUI.Docking.DockPanelExtender;

namespace Google.Solutions.IapDesktop.Application.Services
{
    public interface IThemeService
    {
        DockPanelColorPalette ColorPalette { get; }
        void ApplyTheme(DockPanel dockPanel);
        void ApplyTheme(ToolStrip toolStrip);
    }

    public class ThemeService : IThemeService
    {
        private readonly ThemeBase theme;

        public ThemeService()
        {
            this.theme = new VS2015LightTheme();
            this.theme.Extender.FloatWindowFactory = new CustomFloatWindowFactory();
            this.theme.Extender.DockPaneFactory = new DockPaneFactory(this.theme.Extender.DockPaneFactory);
        }

        //---------------------------------------------------------------------
        // IThemeService.
        //---------------------------------------------------------------------

        public DockPanelColorPalette ColorPalette => this.theme.ColorPalette;

        public void ApplyTheme(DockPanel dockPanel)
        {
            dockPanel.Theme = theme;
        }

        public void ApplyTheme(ToolStrip toolStrip)
        {
            this.theme.ApplyTo(toolStrip);
        }

        //---------------------------------------------------------------------
        // FloatWindowFactory.
        //---------------------------------------------------------------------

        public class CustomFloatWindow : FloatWindow // TODO: Rename
        {
            public CustomFloatWindow(DockPanel dockPanel, DockPane pane)
                : base(dockPanel, pane)
            {
            }

            public CustomFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
                : base(dockPanel, pane, bounds)
            {
            }
        }

        public class CustomFloatWindowFactory : DockPanelExtender.IFloatWindowFactory
        {
            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
            {
                return new CustomFloatWindow(dockPanel, pane, bounds);
            }

            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
            {
                return new CustomFloatWindow(dockPanel, pane);
            }
        }

        private class DockPaneFactory : IDockPaneFactory
        {
            private readonly IDockPaneFactory factory;

            public DockPaneFactory(IDockPaneFactory factory)
            {
                Debug.Assert(factory != null);
                this.factory = factory;
            }

            public DockPane CreateDockPane(IDockContent content, DockState visibleState, bool show)
            {
                return this.factory.CreateDockPane(content, visibleState, show);
            }

            public DockPane CreateDockPane(IDockContent content, FloatWindow floatWindow, bool show)
            {
                return this.factory.CreateDockPane(content, floatWindow, show);
            }

            public DockPane CreateDockPane(IDockContent content, DockPane prevPane, DockAlignment alignment,
                                           double proportion, bool show)
            {
                return this.factory.CreateDockPane(content, prevPane, alignment, proportion, show);
            }

            public DockPane CreateDockPane(IDockContent content, Rectangle floatWindowBounds, bool show)
            {
                
                if (content is DocumentWindow docWindow)
                {
                    var form = docWindow.DockHandler.DockPanel.FindForm();
                    var nonClientOverhead = new Size
                    {
                        Width = form.Width - form.ClientRectangle.Width,
                        Height = form.Height - form.ClientRectangle.Height
                    };


                    return this.factory.CreateDockPane(
                        content,
                        new Rectangle(
                            docWindow.Bounds.Location,
                            docWindow.Bounds.Size + nonClientOverhead),
                        show);
                }
                else
                {
                    return this.factory.CreateDockPane(content, floatWindowBounds, show);
                }
            }
        }
    }
}
