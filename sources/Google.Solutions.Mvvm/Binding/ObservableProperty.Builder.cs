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

using System;

namespace Google.Solutions.Mvvm.Binding
{
    public static class ObservableProperty
    {
        /// <summary>
        /// Create a non-thread safe property.
        /// <returns></returns>
        public static ObservableProperty<T> Build<T>(T initialValue)
        {
            return new ObservableProperty<T>(initialValue);
        }

        /// <summary>
        /// Create a property that guarantees that events are delivered
        /// on a certain thread context.
        /// <returns></returns>
        public static ObservableProperty<T> Build<T>(
            T initialValue,
            ViewModelBase viewModel)
        {
            return new ThreadSafeObservableProperty<T>(viewModel, initialValue);
        }

        public static ObservableFunc<TResult> Build<T1, TResult>(
            ObservableProperty<T1> source,
            Func<T1, TResult> func)
        {
            return new ObservableFunc<TResult>(
                () => func(source.Value),
                source);
        }

        public static ObservableFunc<TResult> Build<T1, T2, TResult>(
            ObservableProperty<T1> source1,
            ObservableProperty<T2> source2,
            Func<T1, T2, TResult> func)
        {
            return new ObservableFunc<TResult>(
                () => func(source1.Value, source2.Value),
                source1,
                source2);
        }

        public static ObservableFunc<TResult> Build<T1, T2, T3, TResult>(
            ObservableProperty<T1> source1,
            ObservableProperty<T2> source2,
            ObservableProperty<T3> source3,
            Func<T1, T2, T3, TResult> func)
        {
            return new ObservableFunc<TResult>(
                () => func(source1.Value, source2.Value, source3.Value),
                source1,
                source2,
                source3);
        }

        public static ObservableFunc<TResult> Build<T1, T2, T3, T4, TResult>(
            ObservableProperty<T1> source1,
            ObservableProperty<T2> source2,
            ObservableProperty<T3> source3,
            ObservableProperty<T4> source4,
            Func<T1, T2, T3, T4, TResult> func)
        {
            return new ObservableFunc<TResult>(
                () => func(source1.Value, source2.Value, source3.Value, source4.Value),
                source1,
                source2,
                source3,
                source4);
        }
    }
}
