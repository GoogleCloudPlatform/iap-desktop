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
using System;

namespace Google.Solutions.Mvvm.Test.Format
{
    [TestFixture]
    public class TestMarkdownReader
    {

        [Test]
        public void WhenStringContainsCarriageReturns_ThenCarriageReturnsAreRemoved()
        {
            var reader = new Markdown.Reader("a\rb\r\n");
            Assert.AreEqual('a', reader.Consume());
            Assert.AreEqual('b', reader.Consume());
            Assert.AreEqual('\n', reader.Consume());
        }

        //---------------------------------------------------------------------
        // Reader: EOF/BOF.
        //---------------------------------------------------------------------

        [Test]
        public void WhenAtBeginning_ThenIsBofReturnsTrue()
        {
            var reader = new Markdown.Reader("");
            Assert.IsTrue(reader.IsBof);
        }

        [Test]
        public void WhenAtEnd_ThenIsEofReturnsTrue()
        {
            var reader = new Markdown.Reader("");
            Assert.IsTrue(reader.IsEof);
        }

        //---------------------------------------------------------------------
        // Reader: PeekAhead.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotAtEnd_ThenPeekAheadReturnsCharacter()
        {
            var reader = new Markdown.Reader("abc");
            Assert.AreEqual('a', reader.PeekAhead());
            Assert.AreEqual('a', reader.PeekAhead());

            reader.Consume();
            Assert.AreEqual('b', reader.PeekAhead());
        }

        [Test]
        public void WhenAtEnd_ThenPeekAheadThrowsException()
        {
            var reader = new Markdown.Reader("abc");
            reader.Consume();
            reader.Consume();
            reader.Consume();

            Assert.Throws<InvalidOperationException>(() => reader.PeekAhead());
        }

        [Test]
        public void WhenPeekingPastEnd_ThenPeekBehindThrowsException()
        {
            var reader = new Markdown.Reader("abc");
            Assert.AreEqual('a', reader.PeekAhead(1));
            Assert.AreEqual('b', reader.PeekAhead(2));
            Assert.AreEqual('c', reader.PeekAhead(3));
            Assert.Throws<InvalidOperationException>(() => reader.PeekAhead(4));
        }

        //---------------------------------------------------------------------
        // Reader: PeekBehind.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotAtBeginning_ThenPeekBehindReturnsCharacter()
        {
            var reader = new Markdown.Reader("abc");
            reader.Consume();
            Assert.AreEqual('a', reader.PeekBehind());
            Assert.AreEqual('a', reader.PeekBehind());
        }

        [Test]
        public void WhenAtBeginning_ThenPeekBehindThrowsException()
        {
            var reader = new Markdown.Reader("abc");
            Assert.Throws<InvalidOperationException>(() => reader.PeekBehind());
        }

        [Test]
        public void WhenPeekingBehindBeginning_ThenPeekBehindThrowsException()
        {
            var reader = new Markdown.Reader("abc");
            reader.Consume();
            Assert.AreEqual('a', reader.PeekBehind(1));
            Assert.Throws<InvalidOperationException>(() => reader.PeekBehind(2));
        }

        //---------------------------------------------------------------------
        // Reader: Consume.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNotAtEnd_ThenConsumeReturnsNextCharacter()
        {
            var reader = new Markdown.Reader("abc");
            Assert.AreEqual('a', reader.Consume());
            Assert.AreEqual('b', reader.Consume());
            Assert.AreEqual('c', reader.Consume());
        }

        [Test]
        public void WhenAtEnd_ThenConsumeThrowsException()
        {
            var reader = new Markdown.Reader("");
            Assert.Throws<InvalidOperationException>(() => reader.Consume());
        }
    }
}
