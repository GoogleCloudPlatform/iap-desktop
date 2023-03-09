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
using Google.Solutions.Mvvm.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// Extension methods to implement simple two-way data bindings
    /// between WinForms controls and model classes.
    /// </summary>
    public static class BindingExtensions
    {
        public static Binding OnPropertyChange<TObject, TProperty>( // TODO: Change signature
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

        public static Binding OnControlPropertyChange<TControl, TProperty>(// TODO: Change signature
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

        //---------------------------------------------------------------------
        // Binding for bare properties.
        //---------------------------------------------------------------------

        public static void BindProperty<TControl, TProperty, TModel>(// TODO: Change signature
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, TProperty>> modelProperty,
            IContainer container = null)
            where TModel : INotifyPropertyChanged
            where TControl : IComponent
        {
            //
            // Apply initial value.
            //
            var modelValue = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(modelValue);

            var forwardBinding = control.OnControlPropertyChange(
                controlProperty,
                CreateSetter(model, modelProperty));

            var reverseBinding = model.OnPropertyChange(
                modelProperty,
                CreateSetter(control, controlProperty));

            //
            // Wire up these two bindings so that we do not deliver
            // updates in cycles.
            //
            forwardBinding.Peer = reverseBinding;
            reverseBinding.Peer = forwardBinding;

            if (container != null)
            {
                // To ensure that the bindings are disposed, add them to the
                // container of the control.
                container.Add(forwardBinding);
                container.Add(reverseBinding);
            }
        }

        public static void BindReadonlyProperty<TControl, TProperty, TModel>(// TODO: Change signature
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, TProperty>> modelProperty,
            IContainer container = null)
            where TModel : INotifyPropertyChanged
        {
            //
            // Apply initial value.
            //
            var modelValue = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(modelValue);

            var binding = model.OnPropertyChange(
                modelProperty,
                CreateSetter(control, controlProperty));

            if (container != null)
            {
                // To ensure that the bindings are disposed, add them to the
                // container of the control.
                container.Add(binding);
            }
        }

        //---------------------------------------------------------------------
        // Binding for ObservableProperties.
        //---------------------------------------------------------------------

        public static void BindObservableProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, IObservableProperty<TProperty>>> modelProperty,
            IContainer container = null)// TODO: Change signature
            where TControl : IComponent
        {
            //
            // Apply initial value.
            //
            var observable = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(observable.Value);

            Debug.Assert(observable is IObservableWritableProperty<TProperty>);

            var forwardBinding = control.OnControlPropertyChange(
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

            if (container != null)
            {
                // To ensure that the bindings are disposed, add them to the
                // container of the control.
                container.Add(forwardBinding);
                container.Add(reverseBinding);
            }
        }

        public static void BindReadonlyObservableProperty<TControl, TProperty, TModel>(
            this TControl control,
            Expression<Func<TControl, TProperty>> controlProperty,
            TModel model,
            Expression<Func<TModel, IObservableProperty<TProperty>>> modelProperty,
            IBindingContext bindingContext)// TODO: Change signature
            where TControl : IComponent
        {
            //
            // Apply initial value.
            //
            var observable = modelProperty.Compile()(model);
            CreateSetter(control, controlProperty)(observable.Value);

            var binding = new NotifyObservablePropertyChangedBinding<TProperty>(
                observable,
                CreateSetter(control, controlProperty));

            bindingContext.OnBindingCreated(control, binding);
        }
        // TODO: Add preconditions to other methods

        public static void BindCommand<TCommand, TModel>(
            this ButtonBase button,
            TModel model,
            Func<TModel, TCommand> commandProperty,
            Func<TModel, IObservableProperty<CommandState>> modelStateProperty,
            IBindingContext bindingContext)
            where TCommand : ICommand<TModel>
        {
            Precondition.NotNull(commandProperty, nameof(commandProperty));
            Precondition.NotNull(model, nameof(model));
            Precondition.NotNull(bindingContext, nameof(bindingContext));

            //
            // Bind status.
            //
            if (modelStateProperty != null)
            {
                var stateObservable = modelStateProperty(model);
                button.Enabled = stateObservable.Value == CommandState.Enabled;

                //
                // Update control if command state changes.
                //
                var stateBinding = new NotifyObservablePropertyChangedBinding<CommandState>(
                    stateObservable,
                    state => button.Enabled = state == CommandState.Enabled);
                bindingContext.OnBindingCreated(button, stateBinding);
            }

            //
            // Forward click events to the command.
            //
            async void OnClickAsync(object _, EventArgs __)
            {
                button.Enabled = false;
                var command = commandProperty(model);
                try
                {
                    await command
                        .ExecuteAsync(model)
                        .ConfigureAwait(true);

                    if (button.FindForm() is Form form &&
                        form.AcceptButton == button)
                    {
                        //
                        // This is the accept button, so treat the
                        // successful command execution as dialog result.
                        //
                        form.DialogResult = DialogResult.OK;
                    }
                }
                catch (Exception e)
                {
                    bindingContext.OnCommandFailed(button, command, e);
                }
                finally
                {
                    button.Enabled = command.QueryState(model) == CommandState.Enabled;
                }
            }

            var clickBinding = new ClickBinding(button, OnClickAsync);
            bindingContext.OnBindingCreated(button, clickBinding);
        }

        

        // TODO: Update other methods to use IBindingContext
        // TODO: Remove Component

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public abstract class Binding : Component
        {
            public bool IsBusy { get; internal set; } = false;
            public Binding Peer { get; internal set; }
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

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.eventInfo.RemoveEventHandler(
                        this.observed,
                        new EventHandler(Observed_PropertyChanged));
                }
            }
        }

        private class NotifyPropertyChangedBinding<TObject, TProperty> : Binding, IDisposable
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

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.observed.PropertyChanged -= Observed_PropertyChanged;
                }
            }
        }

        private class NotifyObservablePropertyChangedBinding<TProperty>
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

        private class ClickBinding : Binding
        {
            private readonly ButtonBase observed;
            private readonly EventHandler handler;

            public ClickBinding(
                ButtonBase observed,
                EventHandler handler)
            {
                this.observed = observed;
                this.handler = handler;

                observed.Click += handler;
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.observed.Click -= this.handler;
                }
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
