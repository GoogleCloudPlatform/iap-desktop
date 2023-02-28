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
using System.Linq;

namespace Google.Solutions.Mvvm.Test.Format
{
    [TestFixture]
    public class TestMarkdownBlock
    {
        //---------------------------------------------------------------------
        // Document.
        //---------------------------------------------------------------------

        [Test]
        public void EmptyDocument()
        {
            var doc = MarkdownBlock.Parse("");
            Assert.IsNotNull(doc);
        }

        //---------------------------------------------------------------------
        // Heading.
        //---------------------------------------------------------------------

        [Test]
        public void Headings()
        {
            var doc = MarkdownBlock.Parse(
                "\n" +
                "# heading1    \n" +
                "##      \theading 2\n" +
                "### heading3\n" +
                "#### heading4 \t\n" +
                "##### heading5\n" +
                "\n" +
                "####### heading6");
            Assert.IsNotNull(doc);
            Assert.AreEqual(
                "[Document]\n" +
                " [ParagraphBreak]\n" +
                " [Heading level=1] heading1\n" +
                " [Heading level=2] heading 2\n" +
                " [Heading level=3] heading3\n" +
                " [Heading level=4] heading4\n" +
                " [Heading level=5] heading5\n" +
                " [ParagraphBreak]\n" +
                " [Heading level=7] heading6\n",
                doc.ToString());
        }

        //---------------------------------------------------------------------
        // TextBlock.
        //---------------------------------------------------------------------

        [Test]
        public void SingleLineTextBlocks()
        {
            var doc = MarkdownBlock.Parse(
                "\n" +
                "block one\n" +
                "\n" +
                "block two" +
                "\n");
            Assert.IsNotNull(doc);
            Assert.AreEqual(
                "[Document]\n" +
                " [ParagraphBreak]\n" +
                " [Text] block one\n" +
                " [ParagraphBreak]\n" +
                " [Text] block two\n",
                doc.ToString());
        }

        [Test]
        public void MultiLineTextBlocks()
        {
            var doc = MarkdownBlock.Parse(
                "\n" +
                "block one, line 1\n" +
                "block one, line 2\n" +
                "\n" +
                "block two, line 1\n" +
                "block two, line 2");
            Assert.IsNotNull(doc);
            Assert.AreEqual(
                "[Document]\n" +
                " [ParagraphBreak]\n" +
                " [Text] block one, line 1 block one, line 2\n" +
                " [ParagraphBreak]\n" +
                " [Text] block two, line 1 block two, line 2\n",
                doc.ToString());
        }

        //---------------------------------------------------------------------
        // UnorderedListItemBlock.
        //---------------------------------------------------------------------

        [Test]
        public void UnorderedListItemBlock()
        {
            var doc = MarkdownBlock.Parse(
                 "* item1a\n" +
                 "  item1b\n" +
                 "* item2a" +
                 "  item2b");
            Assert.IsNotNull(doc);
            Assert.AreEqual(
                "[Document]\n" +
                " [UnorderedListItem bullet=*]\n" +
                "  [Text] item1a item1b\n" +
                " [UnorderedListItem bullet=*]\n" +
                "  [Text] item2a  item2b\n",
                doc.ToString());
        }

        [Test]
        public void MultipleUnorderedListItemBlock()
        {
            var doc = MarkdownBlock.Parse(
                 "* item1a\n" +
                 "  \n" +
                 "* item2a" +
                 "  item2b");
            Assert.IsNotNull(doc);
            Assert.AreEqual(
                "[Document]\n" +
                " [UnorderedListItem bullet=*]\n" +
                "  [Text] item1a item1b\n" +
                " [UnorderedListItem bullet=*]\n" +
                "  [Text] item2a  item2b\n",
                doc.ToString());
        }
    }
}
