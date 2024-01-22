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
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Mvvm.Binding.Commands
{
    /// <summary>
    /// A command that is associated with a fixed context,
    /// typically surfaced as a button.
    /// </summary>
    public interface IObservableCommand : ICommand
    {
        /// <summary>
        /// Check if command can be executed.
        /// </summary>
        IObservableProperty<bool> CanExecute { get; }

        /// <summary>
        /// Text to show in error messages.
        /// </summary>
        new string ActivityText { get; set; }
    }

    public class ObservableCommand : CommandBase, IObservableCommand
    {
        private readonly Func<CancellationToken, Task> executeFunc;

        private ObservableCommand(
            string text,
            Func<CancellationToken, Task> executeFunc,
            IObservableProperty<bool> canExecute)
            : base(text)
        {
            this.CanExecute = canExecute;
            this.executeFunc = executeFunc;
        }

        //TODO: Does removing this override impact GA tracking?
        //public override string Id => null;

        public IObservableProperty<bool> CanExecute { get; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return this.executeFunc(cancellationToken);
        }

        public new string ActivityText
        {
            get => base.ActivityText;
            set => base.ActivityText = value;
        }

        //---------------------------------------------------------------------
        // Builder methods.
        //---------------------------------------------------------------------

        public static ObservableCommand Build(
            string text,
            Func<CancellationToken, Task> executeFunc,
            IObservableProperty<bool>? canExecute = null)
        {
            return new ObservableCommand(
                text,
                executeFunc,
                canExecute ?? ObservableProperty.Build(true));
        }

        public static ObservableCommand Build(
            string text,
            Func<Task> executeFunc,
            IObservableProperty<bool>? canExecute = null)
        {
            return new ObservableCommand(
                text,
                _ => executeFunc(),
                canExecute ?? ObservableProperty.Build(true));
        }

        public static ObservableCommand Build(
            string text,
            Action executeAction,
            IObservableProperty<bool>? canExecute = null)
        {
            return new ObservableCommand(
                text,
                _ =>
                {
                    executeAction();
                    return Task.CompletedTask;
                },
                canExecute ?? ObservableProperty.Build(true));
        }
    }
}
