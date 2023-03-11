﻿//
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

using System;
using System.ComponentModel;


namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Observable property that is derived from one or more
    /// other properties.
    /// </summary>
    public class ObservableFunc<T> : IObservableProperty<T>
    {
        private readonly Func<T> func;

        public event PropertyChangedEventHandler PropertyChanged;

        public T Value => this.func();

        internal ObservableFunc(
            Func<T> func,
            params ISourceProperty[] sources)
        {
            this.func = func;

            foreach (var source in sources)
            {
                source.AddDependentProperty(this);
            }
        }

        public void RaisePropertyChange()
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs("Value"));
        }
    }
}
