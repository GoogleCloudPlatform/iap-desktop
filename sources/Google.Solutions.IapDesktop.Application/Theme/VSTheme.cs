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
    }
}
