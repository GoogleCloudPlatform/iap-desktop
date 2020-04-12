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

using NUnit.Framework;
using System;
using System.Reflection;

namespace Google.Solutions.IapDesktop.Application.Test
{
    [TestFixture]
    public class TestExceptionExtensions
    {
        [Test]
        public void WhenRegularException_UnwrapDoesNothing()
        {
            var ex = new ApplicationException();

            var unwrapped = ex.Unwrap();

            Assert.AreSame(ex, unwrapped);
        }

        [Test]
        public void WhenAggregateException_UnwrapReturnsFirstInnerException()
        {
            var inner1 = new ApplicationException();
            var inner2 = new ApplicationException();
            var aggregate = new AggregateException(inner1, inner2);

            var unwrapped = aggregate.Unwrap();

            Assert.AreSame(inner1, unwrapped);
        }

        [Test]
        public void WhenTargetInvocationException_UnwrapReturnsInnerException()
        {
            var inner = new ApplicationException();
            var target = new TargetInvocationException("", inner);

            var unwrapped = target.Unwrap();

            Assert.AreSame(inner, unwrapped);
        }
    }
}
