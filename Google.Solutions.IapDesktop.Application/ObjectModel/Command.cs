﻿//
// Copyright 2020 Google LLC
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
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.ObjectModel
{
    public interface ICommand<TContext>
    {
        string Text { get; }
        System.Drawing.Image Image { get; }
        Keys ShortcutKeys { get; }

        CommandState QueryState(TContext context);
        void Execute(TContext context);
    }

    public enum CommandState
    {
        Enabled,
        Disabled,
        Unavailable
    }

    /// <summary>
    /// Helper class for creating simple commands.
    /// </summary>
    public class Command<TContext> : ICommand<TContext>
    {
        private Action<TContext> executeFunc;
        private Func<TContext, CommandState> queryStateFunc;

        public Command(
            string text,
            Func<TContext, CommandState> queryStateFunc,
            Action<TContext> executeFunc)
        {
            this.Text = text;
            this.queryStateFunc = queryStateFunc;
            this.executeFunc = executeFunc;
        }

        public string Text { get; }
        public Image Image { get; set; }
        public Keys ShortcutKeys { get; set; }

        public void Execute(TContext context)
        {
            this.executeFunc(context);
        }

        public CommandState QueryState(TContext context)
        {
            return this.queryStateFunc(context);
        }
    }
}
