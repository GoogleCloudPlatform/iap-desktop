//
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
using System.Collections.Generic;
using System.ComponentModel;

namespace Google.Solutions.Mvvm.Binding
{
    public interface IObservableProperty
    {
        void RaisePropertyChange();
    }

    public interface IObservableProperty<T> : IObservableProperty, INotifyPropertyChanged
    {
        T Value { get; }
    }

    public interface IObservableWritableProperty<T> : IObservableProperty, INotifyPropertyChanged
    {
        T Value { get; set; }
    }

    public interface ISourceProperty : IObservableProperty
    {
        void AddDependentProperty(IObservableProperty property);
    }

    public abstract class ObservablePropertyBase<T>
         : IObservableProperty<T>, IObservableWritableProperty<T>, ISourceProperty
    {
        private LinkedList<IObservableProperty> dependents;
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void RaisePropertyChange()
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs("Value"));

            if (this.dependents != null)
            {
                foreach (var dependent in this.dependents)
                {
                    dependent.RaisePropertyChange();
                }
            }
        }

        public void AddDependentProperty(IObservableProperty property)
        {
            if (this.dependents == null)
            {
                this.dependents = new LinkedList<IObservableProperty>();
            }

            this.dependents.AddLast(property);
        }

        public abstract T Value { get; set; }
    }

    /// <summary>
    /// Simple observable property.
    /// </summary>
    public class ObservableProperty<T> : ObservablePropertyBase<T>
    {
        private T value;

        internal ObservableProperty(T initialValue)
        {
            this.value = initialValue;
        }

        /// <summary>
        /// Get or set the value, raises a change event.
        /// </summary>
        public override T Value
        {
            get => this.value;
            set
            {
                this.value = value;
                RaisePropertyChange();
            }
        }
    }

    internal class ThreadSafeObservableProperty<T> : ObservableProperty<T>
    {
        private readonly ISynchronizeInvoke synchronizeInvoke;
        private T value;

        internal ThreadSafeObservableProperty(
            ISynchronizeInvoke synchronizeInvoke,
            T initialValue)
            : base(initialValue)
        {
            this.synchronizeInvoke = synchronizeInvoke;
        }

        /// <summary>
        /// Get or set the value, raises a change event.
        /// </summary>
        public override T Value
        {
            get
            {
                lock (this.synchronizeInvoke)
                {
                    return this.value;
                }
            }
            set
            {
                lock (this.synchronizeInvoke)
                {
                    this.value = value;
                    RaisePropertyChange();
                }
            }
        }

        public override void RaisePropertyChange()
        {
            if (this.synchronizeInvoke.InvokeRequired)
            {
                //
                // We're on the wrong thread (not the GUI thread,
                // presumably).
                //
                this.synchronizeInvoke.BeginInvoke(
                    (Action)(() => base.RaisePropertyChange()),
                    null);
            }
            else
            {
                base.RaisePropertyChange();
            }
        }
    }
}
