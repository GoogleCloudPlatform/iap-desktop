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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Common.Util;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Google.Solutions.Testing.Apis
{
    public static class ExceptionAssert
    {
        /// <summary>
        /// Assert that the delegate throws an exception, possibly
        /// wrapped in an AggregateException.
        /// </summary>
        public static async Task<TActual> ThrowsAsync<TActual>(
            AsyncTestDelegate code,
            string? message = null)
            where TActual : Exception
        {
            Exception? caughtException = null;

            using (new TestExecutionContext.IsolatedContext())
            {
                try
                {
                    await code().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    caughtException = e.Unwrap();
                }
            }

            Assert.That(
                caughtException, 
                new ExceptionTypeConstraint(typeof(TActual)), 
                message);

            return (TActual)caughtException!;
        }

        public static TActual? ThrowsAggregateException<TActual>(TestDelegate code)
            where TActual : Exception
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
    }

    public static class PropertyAssert
    {
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

        public static void RaisesCollectionChangedNotification(
            INotifyCollectionChanged obj,
            Action action,
            NotifyCollectionChangedAction expected)
        {
            var callbacks = 0;

            void handler(object sender, NotifyCollectionChangedEventArgs args)
            {
                Assert.AreSame(obj, sender);
                if (args.Action == expected)
                {
                    callbacks++;
                }
            }

            obj.CollectionChanged += handler;
            action();
            obj.CollectionChanged -= handler;

            Assert.AreEqual(
                1,
                callbacks,
                $"Expected CollectionChanged callback for {expected}");
        }
    }

    public static class EventAssert
    {
        [SuppressMessage("Usage", "VSTHRD103:Call async methods when in an async method", Justification = "")]
        public static async Task<TArgs> RaisesEventAsync<TArgs>(
            Action<Action<TArgs>> registerEvent,
            TimeSpan timeout)
            where TArgs : EventArgs
        {
            var completionSource = new TaskCompletionSource<TArgs>();
            registerEvent(args => completionSource.SetResult(args));

            if (await Task
                .WhenAny(completionSource.Task, Task.Delay(timeout))
                .ConfigureAwait(true) == completionSource.Task)
            {
                return completionSource.Task.Result;
            }
            else
            {
                throw new AssertionException(
                    "Timeout elapsed before event");
            }
        }

        public static Task<TArgs> RaisesEventAsync<TArgs>(
            Action<Action<TArgs>> registerEvent)
            where TArgs : EventArgs
        {
            return RaisesEventAsync<TArgs>(
                registerEvent,
                TimeSpan.FromSeconds(30));
        }
    }
}
