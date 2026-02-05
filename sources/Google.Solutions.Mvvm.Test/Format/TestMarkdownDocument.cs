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

namespace Google.Solutions.Mvvm.Test.Format
{
    [TestFixture]
    public class TestMarkdownDocument
    {
        //---------------------------------------------------------------------
        // Document.
        //---------------------------------------------------------------------

        [Test]
        public void EmptyDocument()
        {
            var doc = MarkdownDocument.Parse("");
            Assert.IsNotNull(doc);
        }

        //---------------------------------------------------------------------
        // Heading.
        //---------------------------------------------------------------------

        [Test]
        public void IsHeadingNode()
        {
            Assert.IsTrue(MarkdownDocument.HeadingNode.IsHeadingNode("# H1"));
            Assert.IsTrue(MarkdownDocument.HeadingNode.IsHeadingNode("## H12 "));
            Assert.IsTrue(MarkdownDocument.HeadingNode.IsHeadingNode("### H3"));
            Assert.IsTrue(MarkdownDocument.HeadingNode.IsHeadingNode("#### H4  "));
            Assert.IsTrue(MarkdownDocument.HeadingNode.IsHeadingNode("###### H5"));
            Assert.IsTrue(MarkdownDocument.HeadingNode.IsHeadingNode("####### H6"));

            Assert.That(MarkdownDocument.HeadingNode.IsHeadingNode(" # "), Is.False);
            Assert.That(MarkdownDocument.HeadingNode.IsHeadingNode("#"), Is.False);
            Assert.That(MarkdownDocument.HeadingNode.IsHeadingNode("#H1"), Is.False);
            Assert.That(MarkdownDocument.HeadingNode.IsHeadingNode(" # H1"), Is.False);
        }

        [Test]
        public void Heading()
        {
            var doc = MarkdownDocument.Parse(
                "\n" +
                "# heading1    \n" +
                "##      \theading 2\n" +
                "### heading3\n" +
                "#### heading4 \t\n" +
                "##### heading5\n" +
                "\n" +
                "####### heading6");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [ParagraphBreak]\n" +
                " [Heading level=1] heading1\n" +
                " [Heading level=2] heading 2\n" +
                " [Heading level=3] heading3\n" +
                " [Heading level=4] heading4\n" +
                " [Heading level=5] heading5\n" +
                " [ParagraphBreak]\n" +
                " [Heading level=7] heading6\n"));
        }

        //---------------------------------------------------------------------
        // TextNode.
        //---------------------------------------------------------------------

        [Test]
        public void SingleLineTextNodes()
        {
            var doc = MarkdownDocument.Parse(
                "\n" +
                "block one\n" +
                "\n" +
                "block two" +
                "\n");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [ParagraphBreak]\n" +
                " [Span]\n" +
                "  [Text] block one\n" +
                " [ParagraphBreak]\n" +
                " [Span]\n" +
                "  [Text] block two\n"));
        }

        [Test]
        public void MultiLineTextNodes()
        {
            var doc = MarkdownDocument.Parse(
                "\n" +
                "block one, line 1\n" +
                "block one, line 2\n" +
                "\n" +
                "block two, line 1\n" +
                "block two, line 2");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [ParagraphBreak]\n" +
                " [Span]\n" +
                "  [Text] block one, line 1 block one, line 2\n" +
                " [ParagraphBreak]\n" +
                " [Span]\n" +
                "  [Text] block two, line 1 block two, line 2\n"));
        }

        //---------------------------------------------------------------------
        // UnorderedListItemNode.
        //---------------------------------------------------------------------

        [Test]
        public void IsUnorderedListItemNode()
        {
            Assert.IsTrue(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("* i"));
            Assert.IsTrue(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("*   i"));
            Assert.IsTrue(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("*\ti"));
            Assert.IsTrue(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("- i"));
            Assert.IsTrue(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("+ i"));
            Assert.IsTrue(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("* i"));

            Assert.That(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode(" * i"), Is.False);
            Assert.That(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode(" *i"), Is.False);
            Assert.That(MarkdownDocument.UnorderedListItemNode.IsUnorderedListItemNode("** i"), Is.False);
        }

        [Test]
        public void UnorderedListItemNode()
        {
            var doc = MarkdownDocument.Parse(
                 "* item1a\n" +
                 "  item1b\n" +
                 "* item2a" +
                 "  item2b");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [UnorderedListItem bullet=* indent=2]\n" +
                "  [Span]\n" +
                "   [Text] item1a item1b\n" +
                " [UnorderedListItem bullet=* indent=2]\n" +
                "  [Span]\n" +
                "   [Text] item2a  item2b\n"));
        }

        [Test]
        public void MultipleUnorderedListItemNodes()
        {
            var doc = MarkdownDocument.Parse(
                 "* item1a\n" +
                 "  \n" +
                 "- item2a\n" +
                 "  item2b");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [UnorderedListItem bullet=* indent=2]\n" +
                "  [Span]\n" +
                "   [Text] item1a\n" +
                "  [ParagraphBreak]\n" +
                " [UnorderedListItem bullet=- indent=2]\n" +
                "  [Span]\n" +
                "   [Text] item2a item2b\n"));
        }

        [Test]
        public void NestedUnorderedListItemNode()
        {
            var doc = MarkdownDocument.Parse(
                 "- item1a\n" +
                 "  \n" +
                 "  +   item2a\n" +
                 "  +   item2b\n" +
                 "  \n" +
                 "1. item3");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [UnorderedListItem bullet=- indent=2]\n" +
                "  [Span]\n" +
                "   [Text] item1a\n" +
                "  [ParagraphBreak]\n" +
                "  [UnorderedListItem bullet=+ indent=4]\n" +
                "   [Span]\n" +
                "    [Text] item2a\n" +
                "  [UnorderedListItem bullet=+ indent=4]\n" +
                "   [Span]\n" +
                "    [Text] item2b\n" +
                "  [ParagraphBreak]\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item3\n"));
        }

        //---------------------------------------------------------------------
        // OrderedListItemNode.
        //---------------------------------------------------------------------

        [Test]
        public void IsOrderedListItemNode()
        {
            Assert.IsTrue(MarkdownDocument.OrderedListItemNode.IsOrderedListItemNode("1. i"));
            Assert.IsTrue(MarkdownDocument.OrderedListItemNode.IsOrderedListItemNode("123345.        \ti"));
            Assert.IsTrue(MarkdownDocument.OrderedListItemNode.IsOrderedListItemNode("0. i"));

            Assert.That(MarkdownDocument.OrderedListItemNode.IsOrderedListItemNode("-1. i"), Is.False);
            Assert.That(MarkdownDocument.OrderedListItemNode.IsOrderedListItemNode("1 i"), Is.False);
        }

        [Test]
        public void OrderedListItemNode()
        {
            var doc = MarkdownDocument.Parse(
                 "1. item1a\n" +
                 "   item1b\n" +
                 "1. item2a\n" +
                 "   item2b");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item1a item1b\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item2a item2b\n"));
        }

        [Test]
        public void MultipleOrderedListItemNodes()
        {
            var doc = MarkdownDocument.Parse(
                 "1. item1a\n" +
                 "  \n" +
                 "1. item2a\n" +
                 "   item2b\n" +
                 "notanitem");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item1a\n" +
                " [ParagraphBreak]\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item2a item2b\n" +
                " [Span]\n" +
                "  [Text] notanitem\n"));
        }

        [Test]
        public void NestedOrderedListItemNode()
        {
            var doc = MarkdownDocument.Parse(
                 "1. item1a\n" +
                 "\n" +
                 "   1. item2a\n" +
                 "      item2b\n" +
                 "1. item3");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item1a\n" +
                "  [ParagraphBreak]\n" +
                "  [OrderedListItem indent=3]\n" +
                "   [Span]\n" +
                "    [Text] item2a item2b\n" +
                " [OrderedListItem indent=3]\n" +
                "  [Span]\n" +
                "   [Text] item3\n"));
        }

        //---------------------------------------------------------------------
        // Emphasis (Underscore).
        //---------------------------------------------------------------------

        [Test]
        public void EmphasisWithUnderscores()
        {
            var doc = MarkdownDocument.Parse(
                 "one _two_ three _four_");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=_] two\n" +
                "  [Text]  three \n" +
                "  [Emphasis delimiter=_] four\n"));
        }

        [Test]
        public void SpuriousUnderscore()
        {
            var doc = MarkdownDocument.Parse(
                 "one _two_ _ three");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=_] two\n" +
                "  [Text]  \n" +
                "  [Text] _ three\n"));
        }

        //---------------------------------------------------------------------
        // Emphasis (Asterisk).
        //---------------------------------------------------------------------

        [Test]
        public void EmphasisWithAsterisks()
        {
            var doc = MarkdownDocument.Parse(
                 "one *two* three *four*");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=*] two\n" +
                "  [Text]  three \n" +
                "  [Emphasis delimiter=*] four\n"));
        }

        [Test]
        public void SpuriousAsterisk()
        {
            var doc = MarkdownDocument.Parse(
                 "one *two* * three");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=*] two\n" +
                "  [Text]  \n" +
                "  [Text] * three\n"));
        }

        [Test]
        public void SingleCharacterEmphasis()
        {
            var doc = MarkdownDocument.Parse(
                 "one *!* char");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=*] !\n" +
                "  [Text]  char\n"));
        }

        [Test]
        public void EmphasisAtStartOfLine()
        {
            var doc = MarkdownDocument.Parse(
                "This is\n" +
                "*emphasized*\n" +
                "text");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] This is \n" +
                "  [Emphasis delimiter=*] emphasized\n" +
                "  [Text]  text\n"));
        }

        //---------------------------------------------------------------------
        // StrongEmphasis.
        //---------------------------------------------------------------------

        [Test]
        public void StrongEmphasis()
        {
            var doc = MarkdownDocument.Parse(
                 "one **two** three **four**");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=**] two\n" +
                "  [Text]  three \n" +
                "  [Emphasis delimiter=**] four\n"));
        }

        [Test]
        public void SpuriousDoubleAsterisk()
        {
            var doc = MarkdownDocument.Parse(
                 "one ** three");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Text] ** three\n"));
        }


        //---------------------------------------------------------------------
        // Code.
        //---------------------------------------------------------------------

        [Test]
        public void Code()
        {
            var doc = MarkdownDocument.Parse(
                 "one `two` three `four`");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] one \n" +
                "  [Emphasis delimiter=`] two\n" +
                "  [Text]  three \n" +
                "  [Emphasis delimiter=`] four\n"));
        }

        //---------------------------------------------------------------------
        // Link.
        //---------------------------------------------------------------------

        [Test]
        public void Link()
        {
            var doc = MarkdownDocument.Parse(
                 "a [link](href).");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] a \n" +
                "  [Link href=href]\n" +
                "   [Text] link\n" +
                "  [Text] .\n"));
        }

        [Test]
        public void LinkWithEmphasis()
        {
            var doc = MarkdownDocument.Parse(
                 "a [link **emph** *and* _more_](href).");
            Assert.IsNotNull(doc);
            Assert.That(
                doc.ToString(), Is.EqualTo("[Document]\n" +
                " [Span]\n" +
                "  [Text] a \n" +
                "  [Link href=href]\n" +
                "   [Text] link \n" +
                "   [Emphasis delimiter=**] emph\n" +
                "   [Text]  \n   [Emphasis delimiter=*] and\n" +
                "   [Text]  \n   [Emphasis delimiter=_] more\n" +
                "  [Text] .\n"));
        }

        //---------------------------------------------------------------------
        // Tokenize.
        //---------------------------------------------------------------------

        [Test]
        public void Tokenize_WhenTextEmpty_ThenTokenizeReturnsNoTokens()
        {
            var tokens = MarkdownDocument.Token.Tokenize(string.Empty);
            Assert.That(tokens, Is.Empty);
        }

        [Test]
        public void Tokenize_WhenTextHasNoDelimeter_ThenTokenizeReturnsSingleToken()
        {
            var tokens = MarkdownDocument.Token.Tokenize("t");
            Assert.That(
                tokens, Is.EqualTo(new[]
                {
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, "t")
                }).AsCollection);
        }

        [Test]
        public void Tokenize_WhenTextHasDelimeters_ThenTokenizeReturnsTokens()
        {
            var tokens = MarkdownDocument.Token.Tokenize("t[a]()* *_ text");
            Assert.That(
                tokens, Is.EqualTo(new[]
                {
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, "t"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, "["),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, "a"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, "]"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, "("),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, ")"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, "*"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, " "),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, "*"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, "_"),
                    new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, " text"),
                }).AsCollection);
        }

        [Test]
        public void Tokenize_WhenTokensEquivalent_ThenEqualsReturnsTrue()
        {
            var token1 = new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, "text");
            var token2 = new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, "text");

            Assert.IsTrue(token1.Equals(token2));
            Assert.IsTrue(token1 == token2);
        }

        [Test]
        public void Tokenize_WhenTokensNotEquivalent_ThenEqualsReturnsFalse()
        {
            var token1 = new MarkdownDocument.Token(MarkdownDocument.TokenType.Text, "text");
            var token2 = new MarkdownDocument.Token(MarkdownDocument.TokenType.Delimiter, ")");

            Assert.That(token1.Equals(token2), Is.False);
            Assert.That(token1.Equals(null!), Is.False);
            Assert.That(token1! == token2, Is.False);
            Assert.That(token1! == null!, Is.False);
            Assert.IsTrue(token1! != token2);
        }
    }
}
