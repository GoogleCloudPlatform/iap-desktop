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
using Google.Solutions.Testing.Common;
using NUnit.Framework;

namespace Google.Solutions.Mvvm.Test.Binding
{
    [TestFixture]
    public class TestObservableProperty
    {
        //---------------------------------------------------------------------
        // Property events.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRaisingPropertyChange_ThenEventIsRaised()
        {
            var property = ObservableProperty.Build(string.Empty);

            PropertyAssert.RaisesPropertyChangedNotification(
                property,
                () => property.RaisePropertyChange(),
                "Value");
        }

        [Test]
        public void WhenRaisingPropertyChange_ThenEventIsRaisedForDependents()
        {
            var property = ObservableProperty.Build(string.Empty);
            var dependent1 = ObservableProperty.Build(
                property,
                s => s.ToUpper());
            var dependent2 = ObservableProperty.Build(
                property,
                s => s.ToLower());

            PropertyAssert.RaisesPropertyChangedNotification(
                dependent1,
                () => property.RaisePropertyChange(),
                "Value");
            PropertyAssert.RaisesPropertyChangedNotification(
                dependent2,
                () => property.RaisePropertyChange(),
                "Value");
        }

        [Test]
        public void WhenValueSet_ThenEventIsRaised()
        {
            var property = ObservableProperty.Build(string.Empty);

            PropertyAssert.RaisesPropertyChangedNotification(
                property,
                () => property.Value = "Test",
                "Value");
        }

        [Test]
        public void WhenValueSet_ThenEventIsRaisedForDependents()
        {
            var property = ObservableProperty.Build(string.Empty);
            var dependent1 = ObservableProperty.Build(
                property,
                s => s.ToUpper());
            var dependent2 = ObservableProperty.Build(
                property,
                s => s.ToLower());

            PropertyAssert.RaisesPropertyChangedNotification(
                dependent1,
                () => property.Value = "Test",
                "Value");
            PropertyAssert.RaisesPropertyChangedNotification(
                dependent2,
                () => property.Value = "Test",
                "Value");
        }

        //---------------------------------------------------------------------
        // Build
        //---------------------------------------------------------------------

        [Test]
        public void WhenFuncDependsOnOneSource_ThenFuncIsCalledWithParameters()
        {
            var source = ObservableProperty.Build("One");
            var dependent1 = ObservableProperty.Build(
                source,
                s => s.ToUpper());

            Assert.AreEqual("ONE", dependent1.Value);
        }

        [Test]
        public void WhenFuncDependsOnTwoSources_ThenFuncIsCalledWithParameters()
        {
            var source1 = ObservableProperty.Build("One");
            var source2 = ObservableProperty.Build("Two");
            var dependent1 = ObservableProperty.Build(
                source1,
                source2,
                (s1, s2) => s1 + s2);

            Assert.AreEqual("OneTwo", dependent1.Value);
        }

        //---------------------------------------------------------------------
        // IsDirty
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueNotChanged_ThenIsModifiedReturnsFalse()
        {
            var prop = ObservableProperty.Build("One");
            Assert.IsFalse(prop.IsModified);
        }

        [Test]
        public void WhenValueNotChangedToSameValue_ThenIsModifiedReturnsTrue()
        {
            var prop = ObservableProperty.Build("One");
            prop.Value = prop.Value;
            Assert.IsTrue(prop.IsModified);
        }

        [Test]
        public void WhenValueNotChangedToDifferentValue_TheIsModifiedReturnsTrue()
        {
            var prop = ObservableProperty.Build("One");
            prop.Value = null; ;
            Assert.IsTrue(prop.IsModified);
        }
    }
}
