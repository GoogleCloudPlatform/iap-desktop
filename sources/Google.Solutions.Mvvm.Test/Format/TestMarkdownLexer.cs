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
    public class TestMarkdownLexer
    {
        //---------------------------------------------------------------------
        // AtxHeading.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineStartsWithOneHash_ThenLexerCreatesAtxHeading1()
        {
            var md = "# header1  ";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.AtxHeading1, "header1")
                },
                lexer.Tokens.ToList());
        }

        [Test]
        public void WhenLineStartsWithOneHashButHasNoText_ThenLexerCreatesAtxHeading1()
        {
            var md = "#";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.AtxHeading1, "")
                },
                lexer.Tokens.ToList());
        }

        [Test]
        public void WhenLineStartsWithSixHashes_ThenLexerCreatesAtxHeading6()
        {
            var md = "######   header6  ";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.AtxHeading6, "header6")
                },
                lexer.Tokens.ToList());
        }

        [Test]
        public void WhenLineStartsWithSevenHashes_ThenLexerCreatesAtxHeading6()
        {
            var md = "#######   header7  ";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.AtxHeading6, "header7")
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // ParagraphBreak.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineIsEmpty_ThenLexerCreatesParagraphBreak()
        {
            var md = 
                "p1\n" +
                "\n" +
                "p2\n" +
                "\n" +
                "p3\n" +
                "\n";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, "p1"),
                    new Markdown.Token(Markdown.TokenType.ParagraphBreak),
                    new Markdown.Token(Markdown.TokenType.Text, "p2"),
                    new Markdown.Token(Markdown.TokenType.ParagraphBreak),
                    new Markdown.Token(Markdown.TokenType.Text, "p3"),
                    new Markdown.Token(Markdown.TokenType.ParagraphBreak),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // ParagraphBreak.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineIndented_ThenLexerCreatesIndent()
        {
            var md =
                "indent0\n" +
                "  indent1\n" +
                "    indent2\n" +
                "  indent1\n" +
                "indent0";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, "indent0"),
                    new Markdown.Token(Markdown.TokenType.Indent),
                    new Markdown.Token(Markdown.TokenType.Text, "indent1"),
                    new Markdown.Token(Markdown.TokenType.Indent),
                    new Markdown.Token(Markdown.TokenType.Indent),
                    new Markdown.Token(Markdown.TokenType.Text, "indent2"),
                    new Markdown.Token(Markdown.TokenType.Indent),
                    new Markdown.Token(Markdown.TokenType.Text, "indent1"),
                    new Markdown.Token(Markdown.TokenType.Text, "indent0"),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // LeftBrace/RightBrace.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineHasBraces_ThenLexerCreatesBrace()
        {
            var md = "a[b[c]]";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, "a"),
                    new Markdown.Token(Markdown.TokenType.LeftBrace),
                    new Markdown.Token(Markdown.TokenType.Text, "b"),
                    new Markdown.Token(Markdown.TokenType.LeftBrace),
                    new Markdown.Token(Markdown.TokenType.Text, "c"),
                    new Markdown.Token(Markdown.TokenType.RightBrace),
                    new Markdown.Token(Markdown.TokenType.RightBrace),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // LeftParanthesis/RightParanethesis.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineHasParanetheses_ThenLexerCreatesParanetheses()
        {
            var md = "a(b(c))";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, "a"),
                    new Markdown.Token(Markdown.TokenType.LeftParanthesis),
                    new Markdown.Token(Markdown.TokenType.Text, "b"),
                    new Markdown.Token(Markdown.TokenType.LeftParanthesis),
                    new Markdown.Token(Markdown.TokenType.Text, "c"),
                    new Markdown.Token(Markdown.TokenType.RightParanethesis),
                    new Markdown.Token(Markdown.TokenType.RightParanethesis),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // List item.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineStartsWithPlusOrMinus_ThenLexerCreatesListItem(
            [Values("-", "+")] string bullet)
        {
            var md = 
                $"{bullet}   item1\n" +
                $"  {bullet}\titem2\n" +
                $"{bullet}notanitem";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.ListItem, bullet),
                    new Markdown.Token(Markdown.TokenType.Text, "item1"),
                    new Markdown.Token(Markdown.TokenType.Indent),
                    new Markdown.Token(Markdown.TokenType.ListItem, bullet),
                    new Markdown.Token(Markdown.TokenType.Text, "item2"),
                    new Markdown.Token(Markdown.TokenType.Text, $"{bullet}notanitem"),
                },
                lexer.Tokens.ToList());
        }

        [Test]
        public void WhenLineStartsWithAsterisk_ThenLexerCreatesListItem()
        {
            var md =
                $"*\titem1\n" +
                $"  * item2\n" +
                $"*notanitem";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.ListItem, "*"),
                    new Markdown.Token(Markdown.TokenType.Text, "item1"),
                    new Markdown.Token(Markdown.TokenType.Indent),
                    new Markdown.Token(Markdown.TokenType.ListItem, "*"),
                    new Markdown.Token(Markdown.TokenType.Text, "item2"),
                    new Markdown.Token(Markdown.TokenType.Asterisk),
                    new Markdown.Token(Markdown.TokenType.Text, "notanitem"),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // Text.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineIsNotWhitespace_ThenLexerCreatesText()
        {
            var md = 
                " .\n" +
                "a\n" +
                ". ";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, " ."),
                    new Markdown.Token(Markdown.TokenType.Text, "a"),
                    new Markdown.Token(Markdown.TokenType.Text, ". "),
                },
                lexer.Tokens.ToList());
        }

        [Test]
        public void WhenLineContainsLeftBraces_ThenLexerCreatesText()
        {
            var md = "text[brace]";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, "text"),
                    new Markdown.Token(Markdown.TokenType.LeftBrace),
                    new Markdown.Token(Markdown.TokenType.Text, "brace"),
                    new Markdown.Token(Markdown.TokenType.RightBrace),
                },
                lexer.Tokens.ToList());
        }

        [Test]
        public void WhenLineContainsAsterisks_ThenLexerCreatesText()
        {
            var md = "text **bold**";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Text, "text "),
                    new Markdown.Token(Markdown.TokenType.DoubleAsterisk),
                    new Markdown.Token(Markdown.TokenType.Text, "bold"),
                    new Markdown.Token(Markdown.TokenType.DoubleAsterisk),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // Text.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineContainsAsterisks_ThenLexerCreatesAsterisks()
        {
            var md = "* **bold** *";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.ListItem, "*"),
                    new Markdown.Token(Markdown.TokenType.DoubleAsterisk),
                    new Markdown.Token(Markdown.TokenType.Text, "bold"),
                    new Markdown.Token(Markdown.TokenType.DoubleAsterisk),
                    new Markdown.Token(Markdown.TokenType.Text, " "),
                    new Markdown.Token(Markdown.TokenType.Asterisk),
                },
                lexer.Tokens.ToList());
        }

        //---------------------------------------------------------------------
        // Underscore.
        //---------------------------------------------------------------------

        [Test]
        public void WhenLineContainsUnderscores_ThenLexerCreatesUnderscores()
        {
            var md = "_ _italics_ _";
            var lexer = new Markdown.Lexer(new Markdown.Reader(md));

            CollectionAssert.AreEqual(
                new[]
                {
                    new Markdown.Token(Markdown.TokenType.Underscore),
                    new Markdown.Token(Markdown.TokenType.Text, " "),
                    new Markdown.Token(Markdown.TokenType.Underscore),
                    new Markdown.Token(Markdown.TokenType.Text, "italics"),
                    new Markdown.Token(Markdown.TokenType.Underscore),
                    new Markdown.Token(Markdown.TokenType.Text, " "),
                    new Markdown.Token(Markdown.TokenType.Underscore),
                },
                lexer.Tokens.ToList());
        }
    }
}
