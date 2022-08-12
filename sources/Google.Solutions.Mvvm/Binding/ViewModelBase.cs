﻿//
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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Sets the view, enabling the view model to query the most
        /// basic information such as the window handle.
        /// </summary>
        public IWin32Window View { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify observers about a property change.
        /// </summary>
        protected void RaisePropertyChange([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Notify observers about a property change. Using this
        /// overload avoids having to use a (brittle) string to
        /// identify a property.
        /// 
        /// Example:
        /// RaisePropertyChange((MyViewModel m) => m.MyProperty);
        /// </summary>
        protected void RaisePropertyChange<TModel, TProperty>(
            Expression<Func<TModel, TProperty>> modelProperty)
        {
            Debug.Assert(modelProperty.NodeType == ExpressionType.Lambda);
            if (modelProperty.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                RaisePropertyChange(propertyInfo.Name);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }

        public bool HasPropertyChangeListeners => this.PropertyChanged != null;
    }
}
