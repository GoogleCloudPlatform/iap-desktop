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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    /// <summary>
    /// Extension methods to implement simple two-way data bindings
    /// between WinForms controls and model classes.
    /// </summary>
    internal static class BindingExtensions
    {
        public static Binding OnPropertyChange<TObject, TProperty>(
            this TObject observed,
            Expression<Func<TObject, TProperty>> modelProperty,
            Action<TProperty> newValue)
            where TObject : INotifyPropertyChanged
        {
            Debug.Assert(modelProperty.NodeType == ExpressionType.Lambda);
            if (modelProperty.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                return new NotifyPropertyChangedBinding<TObject, TProperty>(
                    observed,
                    propertyInfo.Name,
                    modelProperty.Compile(),
                    newValue);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }

        public static Binding OnControlPropertyChange<TControl, TProperty>(
            this TControl observed,
            Expression<Func<TControl, TProperty>> controlProperty,
            Action<TProperty> newValue)
            where TControl : IComponent
        {
            Debug.Assert(controlProperty.NodeType == ExpressionType.Lambda);
            if (controlProperty.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                // Look for a XxxChanged event.
                var changedEvent = typeof(TControl).GetEvent(propertyInfo.Name + "Changed");
                if (changedEvent == null)
                {
                    throw new ArgumentException(
                        $"Cannot observe {propertyInfo.Name} because class does not " +
                        "provide an appropriate event");
                }

                return new EventHandlerBinding<TControl, TProperty>(
                    observed,
                    changedEvent,
                    controlProperty.Compile(),
                    newValue);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }

        private static Action<TProperty> CreateSetter<TObject, TProperty>(
            TObject obj,
            Expression<Func<TObject, TProperty>> controlProperty)
        {
            Debug.Assert(controlProperty.NodeType == ExpressionType.Lambda);
            if (controlProperty.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                return value => propertyInfo.SetValue(obj, value);
            }
            else
            {
                throw new ArgumentException("Expression does not resolve to a property");
            }
        }

        public static IDisposable BindProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, TProperty>> modelProperty)
            where TModel : INotifyPropertyChanged
            where TControl : IComponent
        {
            var forwardBinding = control.OnControlPropertyChange(
                controlProperty,
                CreateSetter(model, modelProperty));

            var reverseBinding = model.OnPropertyChange(
                modelProperty,
                CreateSetter(control, controlProperty));

            // Wire up these two bindings so that we do not deliver
            // updates in cycles.
            forwardBinding.Peer = reverseBinding;
            reverseBinding.Peer = forwardBinding;

            return new MultiDisposable(forwardBinding, reverseBinding);
        }

        public abstract class Binding : IDisposable
        {
            public bool IsBusy { get; internal set; } = false;
            public Binding Peer { get; internal set; }

            public abstract void Dispose();
        }

        private sealed class EventHandlerBinding<TControl, TProperty> : Binding, IDisposable
            where TControl : IComponent
        {
            private readonly TControl observed;
            private readonly EventInfo eventInfo;
            private readonly Func<TControl, TProperty> readPropertyFunc;
            private readonly Action<TProperty> newValueAction;

            private void Observed_PropertyChanged(object sender, EventArgs e)
            {
                if (this.Peer != null && this.Peer.IsBusy)
                {
                    // Reentrant call - stop here to avoid changes bouncing
                    // back and forth.
                    return;
                }

                try
                {
                    this.IsBusy = true;
                    this.newValueAction(this.readPropertyFunc(this.observed));
                }
                finally
                {
                    this.IsBusy = false;
                }
            }

            public EventHandlerBinding(
                TControl observed,
                EventInfo eventDescriptor,
                Func<TControl, TProperty> readPropertyFunc,
                Action<TProperty> newValueAction)
            {
                this.observed = observed;
                this.eventInfo = eventDescriptor;
                this.readPropertyFunc = readPropertyFunc;
                this.newValueAction = newValueAction;

                this.eventInfo.AddEventHandler(
                    this.observed,
                    new EventHandler(Observed_PropertyChanged));
            }

            public override void Dispose()
            {
                this.eventInfo.RemoveEventHandler(
                    this.observed,
                    new EventHandler(Observed_PropertyChanged));
            }
        }

        private sealed class NotifyPropertyChangedBinding<TObject, TProperty> : Binding, IDisposable
            where TObject : INotifyPropertyChanged
        {
            private readonly TObject observed;
            private readonly string propertyName;

            private readonly Func<TObject, TProperty> readPropertyFunc;
            private readonly Action<TProperty> newValueAction;

            private void Observed_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (this.Peer != null && this.Peer.IsBusy)
                {
                    // Reentrant call - stop here to avoid changes bouncing
                    // back and forth.
                    return;
                }

                if (e.PropertyName == this.propertyName)
                {
                    try
                    {
                        this.IsBusy = true;
                        this.newValueAction(this.readPropertyFunc(this.observed));
                    }
                    finally
                    {
                        this.IsBusy = false;
                    }
                }
            }

            public NotifyPropertyChangedBinding(
                TObject observed,
                string propertyName,
                Func<TObject, TProperty> readPropertyFunc,
                Action<TProperty> newValueAction)
            {
                this.observed = observed;
                this.propertyName = propertyName;
                this.readPropertyFunc = readPropertyFunc;
                this.newValueAction = newValueAction;

                this.observed.PropertyChanged += Observed_PropertyChanged;
            }

            public override void Dispose()
            {
                this.observed.PropertyChanged -= Observed_PropertyChanged;
            }
        }

        private sealed class MultiDisposable : IDisposable
        {
            private readonly IEnumerable<IDisposable> disposables;

            public MultiDisposable(params IDisposable[] disposables)
            {
                this.disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var d in this.disposables)
                {
                    d.Dispose();
                }
            }
        }
    }
}
