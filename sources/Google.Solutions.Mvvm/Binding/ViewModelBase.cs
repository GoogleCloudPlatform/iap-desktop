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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// MVVM view model.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// View that the view model has been bound to. Null if
        /// binding has not occurred yet.
        /// </summary>
        public IWin32Window? View { get; set; }

        //---------------------------------------------------------------------
        // INotifyPropertyChanged and helpers.
        //---------------------------------------------------------------------

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notify observers about a property change.
        /// </summary>
        protected void RaisePropertyChange([CallerMemberName] string? propertyName = null)
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

        //---------------------------------------------------------------------
        // Validation.
        //---------------------------------------------------------------------

        internal void Bind(IWin32Window view)
        {
            this.View = view;
            OnValidate();
        }

        internal void Unbind()
        {
            Debug.Assert(this.View != null);
            this.View = null;
        }

        /// <summary>
        /// Check if the view model has been sufficiently initialized to be
        /// bound to a view.
        /// </summary>
        protected virtual void OnValidate()
        { }

        //---------------------------------------------------------------------
        // IDispose and helpers.
        //---------------------------------------------------------------------

        protected bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            if (!this.Disposed)
            {
                this.Disposed = true;
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            else
            {
                Debug.Fail("Object has been disposed already");
            }
        }
    }
}
