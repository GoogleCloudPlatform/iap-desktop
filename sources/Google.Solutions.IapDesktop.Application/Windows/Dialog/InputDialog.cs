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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application.Theme;
using Google.Solutions.IapDesktop.Core.ObjectModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    /// <summary>
    /// Generic input dialog.
    /// </summary>
    public interface IInputDialog
    {
        DialogResult Prompt(
            IWin32Window? owner,
            InputDialogParameters parameters,
            out string? input);
    }

    public struct InputDialogParameters
    {
        /// <summary>
        /// Dialog title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Caption to show in dialog.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Message to show in dialog.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Cue to show in input field.
        /// </summary>
        public string Cue { get; set; }

        /// <summary>
        /// Mask input as password.
        /// </summary>
        public bool IsPassword { get; set; }

        /// <summary>
        /// Callback for validating input.
        /// </summary>
        public ValidationCallback Validate { get; set; }

        internal void ExpectValid()
        {
            this.Title.ExpectNotNull(nameof(this.Title));
            this.Caption.ExpectNotNull(nameof(this.Caption));
            this.Message.ExpectNotNull(nameof(this.Message));
            this.Validate.ExpectNotNull(nameof(this.Validate));
        }

        /// <summary>
        /// Validate user input.
        /// </summary>
        public delegate void ValidationCallback(
            string input,
            out bool valid,
            out string? warning);
    }

    public class InputDialog : IInputDialog
    {
        private readonly Service<IThemeService> themeService;

        public InputDialog(Service<IThemeService> themeService)
        {
            this.themeService = themeService.ExpectNotNull(nameof(themeService));
        }

        public DialogResult Prompt(
            IWin32Window? owner,
            InputDialogParameters parameters,
            out string? input)
        {
            parameters.ExpectValid();

            using (var dialog = new SystemInputDialog(parameters))
            {
                try
                {
                    this.themeService
                        .GetInstance()?
                        .SystemDialogTheme
                        .ApplyTo(dialog);
                }
                catch (UnknownServiceException)
                { }

                var result = dialog.ShowDialog(owner);
                input = dialog.Value;
                return result;
            }
        }
    }
}
