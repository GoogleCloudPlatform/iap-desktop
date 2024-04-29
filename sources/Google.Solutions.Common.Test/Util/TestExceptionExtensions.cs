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

using Google.Solutions.Common.Util;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Google.Solutions.Common.Test.Util
{
    [TestFixture]
    public class TestExceptionExtensions : CommonFixtureBase
    {
        private static ArgumentException CreateException()
        {
            try
            {
                throw new ArgumentException("sample");
            }
            catch (ArgumentException e)
            {
                return e;
            }
        }

        //---------------------------------------------------------------------
        // Unwrap.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRegularException_ThenUnwrapDoesNothing()
        {
            var ex = new ApplicationException();

            var unwrapped = ex.Unwrap();

            Assert.AreSame(ex, unwrapped);
        }

        [Test]
        public void WhenAggregateException_ThenUnwrapReturnsFirstInnerException()
        {
            var inner1 = new ApplicationException();
            var inner2 = new ApplicationException();
            var aggregate = new AggregateException(inner1, inner2);

            var unwrapped = aggregate.Unwrap();

            Assert.AreSame(inner1, unwrapped);
        }

        [Test]
        public void WhenAggregateExceptionContainsAggregateException_ThenUnwrapReturnsFirstInnerException()
        {
            var inner1 = new ApplicationException();
            var inner2 = new ApplicationException();
            var aggregate = new AggregateException(
                new AggregateException(
                    new TargetInvocationException(inner1)), inner2);

            var unwrapped = aggregate.Unwrap();

            Assert.AreSame(inner1, unwrapped);
        }

        [Test]
        public void WhenAggregateExceptionWithoutInnerException_ThenUnwrapDoesNothing()
        {
            var aggregate = new AggregateException();
            var unwrapped = aggregate.Unwrap();

            Assert.AreSame(aggregate, unwrapped);
        }

        [Test]
        public void WhenTargetInvocationException_ThenUnwrapReturnsInnerException()
        {
            var inner = new ApplicationException();
            var target = new TargetInvocationException("", inner);

            var unwrapped = target.Unwrap();

            Assert.AreSame(inner, unwrapped);
        }

        //---------------------------------------------------------------------
        // FillMessage.
        //---------------------------------------------------------------------

        [Test]
        public void WhenExceptionHasNoInnerException_ThenFullMessageIsSameAsMessage()
        {
            var ex = new ArgumentException("something went wrong!");
            Assert.AreEqual(ex.Message, ex.FullMessage());
        }

        [Test]
        public void WhenExceptionHasInnerException_ThenFullMessageContainsAllMessages()
        {
            var ex = new ArgumentException("One",
                new InvalidOperationException("two",
                    new Exception("three")));
            Assert.AreEqual("One: two: three", ex.FullMessage());
        }

        //---------------------------------------------------------------------
        // ToString.
        //---------------------------------------------------------------------

        [Test]
        public void WhenNoOptionsSet_ThenToStringReturnsStandardTrace()
        {
            var ex = CreateException();
            Assert.AreEqual(ex.ToString(), ex.ToString(ExceptionFormatOptions.None));
        }

        [Test]
        public void WhenIncludeOffsetOptionsSet_ThenToStringIncludesOffsets()
        {
            var ex = CreateException();
            var s = ex.ToString(ExceptionFormatOptions.IncludeOffsets);
            StringAssert.Contains(
                "CreateException() +IL_00",
                s);
        }

        //---------------------------------------------------------------------
        // IsComException.
        //---------------------------------------------------------------------

        [Test]
        public void IsComException()
        {
            Assert.IsTrue(new COMException().IsComException());
            Assert.IsTrue(new InvalidComObjectException().IsComException());
            Assert.IsTrue(new AggregateException(new COMException()).IsComException());
            Assert.IsTrue(new TargetInvocationException(new COMException()).IsComException());
            Assert.IsFalse(new InvalidOperationException("...", new COMException()).IsComException());
        }
    }
}
