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
using Google.Solutions.Mvvm.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Extension methods to implement simple two-way data bindings
    /// between WinForms controls and model classes.
    /// </summary>
    public static class BindingExtensions
    {
        internal static Binding CreatePropertyChangeBinding<TObject, TProperty>(
            TObject observed,
            Expression<Func<TObject, TProperty>> modelProperty,
            Action<TProperty> newValue)
            where TObject : class, INotifyPropertyChanged
        {
            Precondition.ExpectNotNull(observed, nameof(observed));
            Precondition.ExpectNotNull(modelProperty, nameof(modelProperty));

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

        internal static Binding CreateControlPropertyChangeBinding<TControl, TProperty>(
            TControl observed,
            Expression<Func<TControl, TProperty>> controlProperty,
            Action<TProperty> newValue)
            where TControl : class, IComponent
        {
            Precondition.ExpectNotNull(observed, nameof(observed));
            Precondition.ExpectNotNull(controlProperty, nameof(controlProperty));

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

        //---------------------------------------------------------------------
        // OnChange callbacks.
        //---------------------------------------------------------------------

        public static void OnControlPropertyChange<TControl, TProperty>(
            this TControl observed,
            Expression<Func<TControl, TProperty>> controlProperty,
            Action<TProperty> newValue,
            IBindingContext bindingContext)
            where TControl : class, IComponent
        {
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            var binding = CreateControlPropertyChangeBinding(
                observed,
                controlProperty,
                newValue);
            bindingContext.OnBindingCreated(observed, binding);
        }

        public static void OnPropertyChange<TObject, TProperty>(
            this TObject observed,
            Expression<Func<TObject, TProperty>> modelProperty,
            Action<TProperty> newValue,
            IBindingContext bindingContext)
            where TObject : class, INotifyPropertyChanged
        {
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            var binding = CreatePropertyChangeBinding(
                observed,
                modelProperty,
                newValue);

            if (binding is IComponent component)
            {
                bindingContext.OnBindingCreated(component, binding);
            }
        }

        //---------------------------------------------------------------------
        // Binding for bare properties.
        //---------------------------------------------------------------------

        public static void BindProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, TProperty>> modelProperty,
            IBindingContext bindingContext)
            where TModel : class, INotifyPropertyChanged
            where TControl : class, IComponent
        {
            Precondition.ExpectNotNull(controlProperty, nameof(controlProperty));
            Precondition.ExpectNotNull(model, nameof(model));
            Precondition.ExpectNotNull(modelProperty, nameof(modelProperty));
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            //
            // Apply initial value.
            //
            var modelValue = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(modelValue);

            var forwardBinding = CreateControlPropertyChangeBinding(
                control,
                controlProperty,
                CreateSetter(model, modelProperty));

            var reverseBinding = CreatePropertyChangeBinding(
                model,
                modelProperty,
                CreateSetter(control, controlProperty));

            //
            // Wire up these two bindings so that we do not deliver
            // updates in cycles.
            //
            forwardBinding.Peer = reverseBinding;
            reverseBinding.Peer = forwardBinding;

            control.AttachDisposable(forwardBinding);
            control.AttachDisposable(reverseBinding);

            bindingContext.OnBindingCreated(control, forwardBinding);
            bindingContext.OnBindingCreated(control, reverseBinding);
        }

        public static void BindReadonlyProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, TProperty>> modelProperty,
            IBindingContext bindingContext)
            where TModel : class, INotifyPropertyChanged
            where TControl : IComponent
        {
            Precondition.ExpectNotNull(controlProperty, nameof(controlProperty));
            Precondition.ExpectNotNull(model, nameof(model));
            Precondition.ExpectNotNull(modelProperty, nameof(modelProperty));
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            //
            // Apply initial value.
            //
            var modelValue = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(modelValue);

            var binding = CreatePropertyChangeBinding(
                model,
                modelProperty,
                CreateSetter(control, controlProperty));

            control.AttachDisposable(binding);
            bindingContext.OnBindingCreated(control, binding);
        }

        //---------------------------------------------------------------------
        // Binding for ObservableProperties.
        //---------------------------------------------------------------------

        public static void BindObservableProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, IObservableProperty<TProperty>>> modelProperty,
            IBindingContext bindingContext)
            where TControl : class, IComponent
            where TModel: class
        {
            Precondition.ExpectNotNull(controlProperty, nameof(controlProperty));
            Precondition.ExpectNotNull(model, nameof(model));
            Precondition.ExpectNotNull(modelProperty, nameof(modelProperty));
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            //
            // Apply initial value.
            //
            var observable = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(observable.Value);

            Debug.Assert(observable is IObservableWritableProperty<TProperty>);

            var forwardBinding = CreateControlPropertyChangeBinding(
                control,
                controlProperty,
                val => ObservablePropertyHelper.SetValue(observable, val));

            var reverseBinding = new NotifyObservablePropertyChangedBinding<TProperty>(
                observable,
                CreateSetter(control, controlProperty));

            //
            // Wire up these two bindings so that we do not deliver
            // updates in cycles.
            //
            forwardBinding.Peer = reverseBinding;
            reverseBinding.Peer = forwardBinding;

            control.AttachDisposable(forwardBinding);
            control.AttachDisposable(reverseBinding);

            bindingContext.OnBindingCreated(control, forwardBinding);
            bindingContext.OnBindingCreated(control, reverseBinding);
        }

        public static void BindReadonlyObservableProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, IObservableProperty<TProperty>>> modelProperty,
            IBindingContext bindingContext)
            where TControl : IComponent
            where TModel : class
        {
            Precondition.ExpectNotNull(controlProperty, nameof(controlProperty));
            Precondition.ExpectNotNull(model, nameof(model));
            Precondition.ExpectNotNull(modelProperty, nameof(modelProperty));
            Precondition.ExpectNotNull(bindingContext, nameof(bindingContext));

            //
            // Apply initial value.
            //
            var observable = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(observable.Value);

            var binding = new NotifyObservablePropertyChangedBinding<TProperty>(
                observable,
                CreateSetter(control, controlProperty));

            control.AttachDisposable(binding);
            bindingContext.OnBindingCreated(control, binding);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public abstract class Binding : IDisposable
        {
            public bool IsBusy { get; internal set; } = false;
            public Binding? Peer { get; internal set; }
            public abstract void Dispose();
        }

        private sealed class EventHandlerBinding<TControl, TProperty> : Binding
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

        internal class NotifyPropertyChangedBinding<TObject, TProperty> : Binding, IDisposable
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

        internal class NotifyObservablePropertyChangedBinding<TProperty>
            : NotifyPropertyChangedBinding<IObservableProperty<TProperty>, TProperty>
        {
            public NotifyObservablePropertyChangedBinding(
                IObservableProperty<TProperty> observed,
                Action<TProperty> newValueAction)
                : base(
                      observed,
                      "Value",
                      prop => prop.Value,
                      newValueAction)
            {
            }
        }

        private static class ObservablePropertyHelper
        {
            public static void SetValue<TProperty>(
                IObservableProperty<TProperty> property,
                TProperty newValue)
            {
                if (property is IObservableWritableProperty<TProperty> writable)
                {
                    writable.Value = newValue;
                }
                else
                {
                    throw new InvalidOperationException("Observable property is read-only");
                }
            }
        }
    }
}
