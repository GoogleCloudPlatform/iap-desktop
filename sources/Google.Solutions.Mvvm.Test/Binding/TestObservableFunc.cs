//
// Copyright 2022 Google LLC
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

using Google.Solutions.Mvvm.Binding;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestObservableFunc
    {

        [Test]
        public void Value_WhenFuncDependsOnOneSource_ThenFuncIsCalledWithParameters()
        {
            var source = ObservableProperty.Build("One");
            var dependent1 = ObservableProperty.Build(
                source,
                s => s.ToUpper());

            Assert.AreEqual("ONE", dependent1.Value);
        }

        [Test]
        public void Value_WhenFuncDependsOnTwoSources_ThenFuncIsCalledWithParameters()
        {
            var source1 = ObservableProperty.Build("One");
            var source2 = ObservableProperty.Build("Two");
            var dependent1 = ObservableProperty.Build(
                source1,
                source2,
                (s1, s2) => s1 + s2);

            Assert.AreEqual("OneTwo", dependent1.Value);
        }
    }
}
