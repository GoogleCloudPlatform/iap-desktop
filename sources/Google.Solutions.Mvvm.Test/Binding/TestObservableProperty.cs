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
using Google.Solutions.Testing.Apis;
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
        public void RaisePropertyChange()
        {
            var property = ObservableProperty.Build(string.Empty);

            PropertyAssert.RaisesPropertyChangedNotification(
                property,
                () => property.RaisePropertyChange(),
                "Value");
        }

        [Test]
        public void RaisePropertyChange_NotifiesDependents()
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
        public void Value_WhenValueSet_ThenEventIsRaised()
        {
            var property = ObservableProperty.Build(string.Empty);

            PropertyAssert.RaisesPropertyChangedNotification(
                property,
                () => property.Value = "Test",
                "Value");
        }

        [Test]
        public void Value_WhenValueSet_ThenEventIsRaisedForDependents()
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
    }
}
