//
// Copyright 2019 Google LLC
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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Google.Solutions.Common.Test
{
    public static class AssertEx
    {
        public static TActual ThrowsAggregateException<TActual>(TestDelegate code) where TActual : Exception
        {
            return Assert.Throws<TActual>(() =>
            {
                try
                {
                    code();
                }
                catch (AggregateException e)
                {
                    throw e.Unwrap();
                }
            });
        }

        public static void ArePropertiesEqual<T>(T expected, T actual)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var expectedValue = property.GetValue(expected, null);
                var actualValue = property.GetValue(actual, null);

                Assert.AreEqual(expectedValue, actualValue, $"{property.Name} must match");
            }
        }

        public static void RaisesPropertyChangedNotification(
            INotifyPropertyChanged obj,
            Action action,
            string property)
        {
            var callbacks = 0;

            void handler(object sender, PropertyChangedEventArgs args)
            {
                Assert.AreSame(obj, sender);
                if (property == args.PropertyName)
                {
                    callbacks++;
                }
            }

            obj.PropertyChanged += handler;
            action();
            obj.PropertyChanged -= handler;

            Assert.AreEqual(
                1, 
                callbacks, 
                $"Expected PropertyChanged callback for {property}");
        }

        public static void RaisesPropertyChangedNotification<T, TProperty>(
            T obj,
            Action action,
            Expression<Func<T, TProperty>> property) where T : INotifyPropertyChanged
        {
            Debug.Assert(property.NodeType == ExpressionType.Lambda);
            if (property.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                RaisesPropertyChangedNotification(
                    obj,
                    action,
                    propertyInfo.Name);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }
    }
}
