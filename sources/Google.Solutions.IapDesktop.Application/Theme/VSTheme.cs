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

using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Visual Studio theme as defined by a .vstheme file.
    /// </summary>
    internal class VSTheme : VS2015ThemeBase
    {
        public bool IsDark { get; }
        public VSColorPalette Palette { get; }

        private VSTheme(
            VSColorPalette palette,
            bool isDark) : base(palette)
        {
            this.Palette = palette;
            this.IsDark = isDark;
            this.ToolStripRenderer = new VSThemeExtensions.ToolStripRenderer(palette);
            this.Extender.FloatWindowFactory = new VSThemeExtensions.FloatWindowFactory();
            this.Extender.DockPaneFactory =
                new VSThemeExtensions.DockPaneFactory(this.Extender.DockPaneFactory);
        }

        public static VSTheme GetLightTheme()
        {
            return FromResource("Light.vstheme.gz", false);
        }

        public static VSTheme GetDarkTheme()
        {
            return FromResource("Dark.vstheme.gz", true);
        }

        /// <summary>
        /// Read gzip-compressed VSTheme XML file from embedded resource.
        /// </summary>
        public static VSTheme FromResource(string resourceName, bool isDark)
        {
            var assembly = typeof(VSTheme).Assembly;
            var qualifiedResourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceName));

            if (qualifiedResourceName == null)
            {
                throw new IOException(
                    $"The theme {resourceName} does not exist");

            }

            using (var gzipStream = assembly.GetManifestResourceStream(qualifiedResourceName))
            using (var stream = new GZipStream(gzipStream, CompressionMode.Decompress))
            using (var reader = new StreamReader(stream))
            {
                return new VSTheme(
                    new VSColorPalette(XDocument.Load(reader)),
                    isDark);
            }
        }

        //---------------------------------------------------------------------
        // Palette.
        //---------------------------------------------------------------------

        internal class VSColorPalette : DockPanelColorPalette
        {
            public ToolWindowInnerTabPalette ToolWindowInnerTabInactive { get; }
            public GridHeadingPallette GridHeading { get; }
            public WindowPallette Window { get; }
            public ButtonPallette Button { get; }
            public LabelPalette Label { get; }
            public LabelPalette LinkLabel { get; }
            public TextBoxPalette TextBox { get; }
            public ComboBoxPalette ComboBox { get; }
            public ProgressBarPalette ProgressBar { get; }
            public TabControlPalette TabControl { get; }

            public VSColorPalette(XDocument xml) : base(xml)
            {
                this.ToolWindowInnerTabInactive = new ToolWindowInnerTabPalette()
                {
                    Background = GetColor(xml, "CommonControls", "InnerTabInactiveBackground", "Background"),
                    Text = GetColor(xml, "CommonControls", "InnerTabInactiveText", "Background"),
                };
                this.GridHeading = new GridHeadingPallette()
                {
                    Background = GetColor(xml, "Environment", "ToolWindowContentGrid", "Background"),
                    Text = GetColor(xml, "Environment", "GridHeadingText", "Background")
                };
                this.Window = new WindowPallette()
                {
                    Background = GetColor(xml, "Environment", "Window", "Background"),
                    Frame = GetColor(xml, "Environment", "WindowFrame", "Background"),
                    Text = GetColor(xml, "Environment", "WindowText", "Background")
                };
                this.Button = new ButtonPallette()
                {
                    Background = GetColor(xml, "CommonControls", "Button", "Background"),
                    Text = GetColor(xml, "CommonControls", "Button", "Foreground"),
                    Border = GetColor(xml, "CommonControls", "ButtonBorder", "Background"),
                    BorderFocused = GetColor(xml, "CommonControls", "ButtonBorderFocused", "Background"),
                    BorderHover = GetColor(xml, "CommonControls", "ButtonBorderHover", "Background"),
                    BackgroundHover = GetColor(xml, "CommonControls", "ButtonHover", "Background"),
                    BackgroundPressed = GetColor(xml, "CommonControls", "ButtonPressed", "Background"),
                    DropDownGlyphColor = GetColor(xml, "CommonControls", "ComboBoxGlyph", "Background"),
                    DropDownGlyphDisabledColor = GetColor(xml, "CommonControls", "ComboBoxGlyphDisabled", "Background"),
                };
                this.Label = new LabelPalette()
                {
                    Text = GetColor(xml, "Environment", "CaptionText", "Background"),
                };
                this.LinkLabel = new LabelPalette()
                {
                    Text = GetColor(xml, "Environment", "ControlLinkText", "Background"),
                };
                this.TextBox = new TextBoxPalette()
                {
                    Text = GetColor(xml, "CommonControls", "TextBoxText", "Background"),
                    Background = GetColor(xml, "CommonControls", "TextBoxBackground", "Background"),
                    BackgroundDisabled = GetColor(xml, "CommonControls", "TextBoxBackgroundDisabled", "Background"),
                    Border = GetColor(xml, "CommonControls", "TextBoxBorder", "Background"),
                    BorderFocused = GetColor(xml, "CommonControls", "TextBoxBorderFocused", "Background"),
                    BorderHover = GetColor(xml, "CommonControls", "ButtonBorderHover", "Background"),
                };
                this.ComboBox = new ComboBoxPalette()
                {
                    Text = GetColor(xml, "CommonControls", "TextBoxText", "Background"),
                    Background = GetColor(xml, "CommonControls", "ComboBoxBackground", "Background"),
                };
                this.ProgressBar = new ProgressBarPalette()
                {
                    Background = GetColor(xml, "ProgressBar", "Background", "Background"),
                    Indicator = GetColor(xml, "ProgressBar", "IndicatorFill", "Background"),
                };
                this.TabControl = new TabControlPalette()
                {
                    TabBackground = GetColor(xml, "ProjectDesigner", "CategoryTab", "Background"),
                    TabText = GetColor(xml, "ProjectDesigner", "CategoryTab", "Foreground"),
                    SelectedTabBackground = GetColor(xml, "ProjectDesigner", "SelectedCategoryTab", "Background"),
                    SelectedTabText = GetColor(xml, "ProjectDesigner", "SelectedCategoryTab", "Foreground"),
                    MouseOverTabBackground = GetColor(xml, "ProjectDesigner", "MouseOverCategoryTab", "Background"),
                    MouseOverTabText = GetColor(xml, "ProjectDesigner", "MouseOverCategoryTab", "Foreground"),
                };
                this.TabSelectedActiveAccent1 = new TabPalette()
                {
                    Background = GetColor(xml, "Environment", "VizSurfaceDarkGoldDark", "Background"),
                    Text = this.TabSelectedActive.Text,
                    Button = this.TabSelectedActive.Button
                };
                this.TabSelectedActiveAccent2 = new TabPalette()
                {
                    Background = GetColor(xml, "Environment", "VizSurfacePlumDark", "Background"),
                    Text = this.TabSelectedActive.Text,
                    Button = this.TabSelectedActive.Button
                };
                this.TabSelectedActiveAccent3 = new TabPalette()
                {
                    Background = GetColor(xml, "Environment", "VizSurfaceGreenDark", "Background"),
                    Text = this.TabSelectedActive.Text,
                    Button = this.TabSelectedActive.Button
                };
                this.TabSelectedActiveAccent4 = new TabPalette()
                {
                    Background = GetColor(xml, "Environment", "VizSurfaceBrownDark", "Background"),
                    Text = this.TabSelectedActive.Text,
                    Button = this.TabSelectedActive.Button
                };
                this.CommandBarMenuTopLevelHeaderHovered.Border
                    = GetColor(xml, "Environment", "CommandBarMenuItemMouseOverBorder", "Background");
            }

            protected static Color GetColor(
                XDocument xml,
                string category,
                string name,
                string type)
            {
                var color = xml.Root.Element("Theme")
                    .Elements("Category").FirstOrDefault(item => item.Attribute("Name").Value == category)?
                    .Elements("Color").FirstOrDefault(item => item.Attribute("Name").Value == name)?
                    .Element(type).Attribute("Source").Value;
                if (color == null)
                {
                    return Color.Transparent;
                }

                return ColorTranslator.FromHtml($"#{color}");
            }
        }

        internal struct ToolWindowInnerTabPalette
        {
            public Color Background { get; set; }
            public Color Text { get; set; }
        }

        internal struct GridHeadingPallette
        {
            public Color Background { get; set; }
            public Color Text { get; set; }
        }

        internal struct WindowPallette
        {
            public Color Background { get; set; }
            public Color Frame { get; set; }
            public Color Text { get; set; }
        }

        internal struct ButtonPallette
        {
            public Color Background { get; set; }
            public Color Text { get; set; }
            public Color Border { get; set; }
            public Color BorderFocused { get; set; }
            public Color BorderHover { get; set; }
            public Color BackgroundHover { get; set; }
            public Color BackgroundPressed { get; set; }
            public Color DropDownGlyphColor { get; set; }
            public Color DropDownGlyphDisabledColor { get; set; }
        }

        internal struct LabelPalette
        {
            public Color Text { get; set; }
        }

        internal struct TextBoxPalette
        {
            public Color Text { get; set; }
            public Color Background { get; set; }
            public Color BackgroundDisabled { get; set; }
            public Color Border { get; set; }
            public Color BorderFocused { get; set; }
            public Color BorderHover { get; set; }
        }

        internal struct ComboBoxPalette
        {
            public Color Text { get; set; }
            public Color Background { get; set; }
        }

        internal struct ProgressBarPalette
        {
            public Color Indicator { get; set; }
            public Color Background { get; set; }
        }

        internal struct TabControlPalette
        {
            public Color TabBackground { get; set; }
            public Color TabText { get; set; }
            public Color MouseOverTabBackground { get; set; }
            public Color MouseOverTabText { get; set; }
            public Color SelectedTabBackground { get; set; }
            public Color SelectedTabText { get; set; }
        }
    }
}
