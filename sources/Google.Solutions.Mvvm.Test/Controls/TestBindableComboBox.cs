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

using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using NUnit.Framework;
using System.Threading;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestBindableComboBox
    {
        //---------------------------------------------------------------------
        // SelectionAdapter
        //---------------------------------------------------------------------

        public enum Dish
        {
            [System.ComponentModel.Description("Italian dish")]
            Pizza = 0,

            [System.ComponentModel.Description("Chinese dish")]
            Dumplings = 1,

            Leftovers = 2,
            _Default = Pizza
        }

        [Test]
        public void Options_WhenEnumHasValuesWithoutAttribute_ThenValuesAreIgnored()
        {
            var prop = ObservableProperty.Build<Dish>(Dish.Pizza);
            var adapter = new BindableComboBox.SelectionAdapter<Dish>(prop);

            CollectionAssert.AreEquivalent(
                new[] { Dish.Pizza, Dish.Dumplings },
                adapter.Options);
        }

        [Test]
        public void Value_WhenPropertyChanges_ThenSelectedIndexIsUpdated()
        {
            var prop = ObservableProperty.Build<Dish>(Dish.Dumplings);
            var adapter = new BindableComboBox.SelectionAdapter<Dish>(prop);

            Assert.That(adapter.SelectedIndex, Is.EqualTo(1));

            prop.Value = Dish.Pizza;

            Assert.That(adapter.SelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void SelectedIndex_WhenSelectedIndexChanges_ThenPropertyIsUpdated()
        {
            var prop = ObservableProperty.Build<Dish>(Dish.Dumplings);
            var adapter = new BindableComboBox.SelectionAdapter<Dish>(prop)
            {
                SelectedIndex = 0
            };
            Assert.That(adapter.SelectedIndex, Is.EqualTo(0));
            Assert.That(prop.Value, Is.EqualTo(Dish.Pizza));
        }
    }
}
