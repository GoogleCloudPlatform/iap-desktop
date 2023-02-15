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

using System.IO;
using System.Linq;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace Google.Solutions.IapDesktop.Application.Theme
{
    /// <summary>
    /// Visual Studio theme as defined by a .vstheme file.
    /// </summary>
    internal class VSTheme : VS2015ThemeBase
    {
        private VSTheme(byte[] vsthemeXml) : base(vsthemeXml)
        {
        }

        public static VSTheme GetLightTheme()
        {
            return FromResource("Light.vstheme.gz");
        }

        public static VSTheme GetDarkTheme()
        {
            return FromResource("Dark.vstheme.gz");
        }

        /// <summary>
        /// Read gzip-compressed VSTheme XML file from embedded resource.
        /// </summary>
        public static VSTheme FromResource(string resourceName)
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
            using (var stream = assembly.GetManifestResourceStream(qualifiedResourceName))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return new VSTheme(Decompress(buffer.ToArray()));
            }
        }

        /// <summary>
        /// Read VSTheme XML file from a file.
        /// </summary>
        public static VSTheme FromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new IOException(
                    $"The theme file {filePath} does not exist");
            }

            return new VSTheme(File.ReadAllBytes(filePath));
        }
    }
}
