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

using Google.Solutions.Mvvm.Format;
using NUnit.Framework;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Google.Solutions.Mvvm.Test.Format
{
    [TestFixture]
    public class TestRtfWriter
    {
        [Test]
        public void __()
        {
            using (var buffer = new StringWriter())
            using (var writer = new RtfWriter(buffer))
            {
                writer.WriteHeader(FontFamily.GenericSansSerif);

                writer.WriteParagraphStart();
                writer.SetFontSize(36);
                writer.SetBold(true);
                writer.WriteText("Headline");
                writer.SetBold(false);
                writer.SetFontSize(12);
                writer.WriteParagraphEnd();

                writer.WriteParagraphStart();
                writer.SetBold(true);
                writer.WriteText("Hello world");
                writer.SetBold(false);
                writer.WriteParagraphEnd();

                writer.WriteParagraphStart();
                writer.SetItalic(true);
                writer.WriteText("Hello world");
                writer.SetItalic(false);
                writer.WriteParagraphEnd();

                writer.WriteParagraphStart();
                writer.SetUnderline(true);
                writer.WriteText("Hello world");
                writer.SetUnderline(false);
                writer.WriteParagraphEnd();

                writer.WriteParagraphStart();
                writer.SetUnderline(true);
                writer.WriteHyperlink("Google", "https://Google.com/");
                writer.SetUnderline(false);
                writer.WriteParagraphEnd();


                writer.WriteParagraphStart();
                writer.WriteUnorderedListItem(-270, 360);
                writer.WriteText("first\nlevel");
                writer.WriteParagraphEnd();

                writer.WriteParagraphStart();
                writer.WriteUnorderedListItem(-270, 720);
                writer.WriteText("second\nlevel");
                writer.WriteParagraphEnd();


                var s = buffer.ToString();
            }
        }
    }
}
