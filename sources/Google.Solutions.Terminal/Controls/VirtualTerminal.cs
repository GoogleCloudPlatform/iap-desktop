//
// Copyright 2024 Google LLC
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

using Google.Solutions.Common.Runtime;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Interop;
using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Platform.IO;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Windows Terminal.
    /// </summary>
    public partial class VirtualTerminal : UserControl
    {
        private const string DefaultFontFamily = "Consolas";
        private const float DefaultFontSize = 9.75f;
        private const int MinimumFontSizeForScrolling = 6;
        private const int MaximumFontSizeForScrolling = 36;
        private const string Esc = "\u001b";

        private TerminalSafeHandle? terminal = null;
        private SubclassCallback? terminalSubclass;

        private PseudoConsoleSize dimensions;
        private TerminalDeviceBinding? deviceBinding;

        private readonly NativeMethods.WriteCallback writeCallback;
        private readonly NativeMethods.ScrollCallback scrollCallback;

        //
        // NB. The Windows Terminal calls the caret "cursor", which can be
        // confusing in a GUI environment where "cursor" typically refers to
        // the mouse pointer.
        //
        // For consistency's sake, we use the term "caret" here.
        //

        public VirtualTerminal()
        {
            InitializeComponent();

            this.writeCallback = new NativeMethods.WriteCallback(OnUserInput);
            this.scrollCallback = new NativeMethods.ScrollCallback(OnTerminalScrolled);

            if (this.DesignMode)
            {
                return;
            }

            var blinkTime = NativeMethods.GetCaretBlinkTime();
            if (blinkTime == uint.MaxValue)
            {
                //
                // Caret does not blink.
                //
            }
            else
            {
                this.caretBlinkTimer.Tick += (_, __) =>
                {
                    if (this.terminal != null)
                    {
                        NativeMethods.TerminalBlinkCursor(this.terminal);
                    }
                };

                this.caretBlinkTimer.Interval = (int)blinkTime;
                this.caretBlinkTimer.Start();
            }
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Clear the screen.
        /// </summary>
        public void Clear()
        {
            ReceiveOutput(Esc + "[2J");
        }

        /// <summary>
        /// Get or set dimensions of terminal (in characters).
        /// </summary>
        public PseudoConsoleSize Dimensions
        {
            get => this.dimensions;
            internal set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                this.dimensions = value;
                OnDimensionsChanged();
            }
        }

        /// <summary>
        /// Get or set the device to interact with.
        /// </summary>
        public IPseudoConsole? Device
        {
            get => this.deviceBinding?.Device;
            set
            {
                Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

                if (this.deviceBinding != null)
                {
                    Clear();
                    this.deviceBinding.Dispose();
                    this.deviceBinding = null;
                }

                if (value != null)
                {
                    this.deviceBinding = new TerminalDeviceBinding(
                        this, 
                        value);
                }
            }
        }

        //---------------------------------------------------------------------
        // VT binding.
        //---------------------------------------------------------------------

        /// <summary>
        /// Receive data to display. The data can contain xterm control 
        /// characters. 
        /// </summary>
        internal void ReceiveOutput(string data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(((Action)(() => OnOutputReceived(data))));
            }
            else
            {
                OnOutputReceived(data);
            }
        }

        /// <summary>
        /// Process error received from device.
        /// </summary>
        internal void ReceiveError(Exception e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(((Action)(() => OnDeviceError(e))));
            }
            else
            {
                OnDeviceError(e);
            }
        }

        /// <summary>
        /// Process device closure.
        /// </summary>
        internal void ReceiveClose()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(((Action)(() => OnDeviceClosed())));
            }
            else
            {
                OnDeviceClosed();
            }
        }

        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        public event EventHandler? DimensionsChanged;

        /// <summary>
        /// Terminal is about to receive (and display) new data.
        /// </summary>
        public event EventHandler<TerminalOutputEventArgs>? Output;

        /// <summary>
        /// Terminal received user input that needs to be sent to the device.
        /// </summary>
        public event EventHandler<TerminalInputEventArgs>? UserInput;

        /// <summary>
        /// The device has failed.
        /// </summary>
        public event EventHandler<TerminalErrorEventArgs>? DeviceError;

        /// <summary>
        /// Device has been closed, this might be because the user ended the
        /// session.
        /// </summary>
        public event EventHandler? DeviceClosed;

        /// <summary>
        /// Terminal theme changed.
        /// </summary>
        public event EventHandler? ThemeChanged;

        protected virtual void OnDimensionsChanged()
        {
            this.DimensionsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnUserInput(string data)
        { 
            this.UserInput?.Invoke(this, new TerminalInputEventArgs(data));
        }

        protected virtual void OnTerminalScrolled(
            int viewTop,
            int viewHeight,
            int bufferSize)
        {
            //
            // Adjust the scrollbar maximum based on the unseen part, and set the
            // current position.
            //
            this.scrollBar.Minimum = 0;
            this.scrollBar.Maximum = bufferSize - viewHeight;
            this.scrollBar.Value = viewTop;
        }

        protected virtual void OnOutputReceived(string data)
        {
            if (this.DesignMode)
            {
                return;
            }

            if (this.terminal != null)
            {
                NativeMethods.TerminalSendOutput(this.terminal, data);
            }

            this.Output?.Invoke(this, new TerminalOutputEventArgs(data));
        }

        protected virtual void OnDeviceError(Exception e)
        {
            this.DeviceError?.Invoke(this, new TerminalErrorEventArgs(e));
        }

        protected virtual void OnDeviceClosed()
        {
            this.DeviceClosed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnThemeChanged()
        {
            if (this.terminal == null || this.DesignMode)
            {
                //
                // Handle not created yet, so we can ignore theme changes.
                //
                return;
            }

            var theme = new TerminalTheme()
            {
                DefaultBackground = (uint)ColorTranslator.ToWin32(this.BackColor),
                DefaultForeground = (uint)ColorTranslator.ToWin32(this.ForeColor),
                DefaultSelectionBackground = (uint)ColorTranslator.ToWin32(this.SelectionBackColor),
                SelectionBackgroundAlpha = this.SelectionBackgroundAlpha,
                CursorStyle = this.caretStyle,
                ColorTable = this.terminalColors.ToNative()
            };

            NativeMethods.TerminalSetTheme(
                this.terminal,
                theme,
                this.Font.FontFamily.Name,
                (short)this.Font.Size,
                DeviceCapabilities.Current.Dpi);

            this.ThemeChanged?.Invoke(this, EventArgs.Empty);

            //
            // Changing the font might have an impact on dimensions.
            // Trigger a pseudo-resize to cause the terminal to re-calculate
            // dimensions and bring our Dimensions property back into sync.
            //
            OnResize(EventArgs.Empty);
        }

        //---------------------------------------------------------------------
        // Window events.
        //---------------------------------------------------------------------

        private void OnScrollbarScrolled(object sender, ScrollEventArgs e)
        {
            if (this.DesignMode)
            {
                return;
            }

            var terminalHandle = Invariant.ExpectNotNull(this.terminal, "Terminal");

            NativeMethods.TerminalUserScroll(terminalHandle, e.NewValue);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            switch (keyData & ~(Keys.Shift | Keys.Control | Keys.Alt))
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    //
                    // Prevent the scrollbar from processing these keys.
                    //
                    return false;

                default:
                    return base.ProcessDialogKey(keyData);
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.DesignMode)
            {
                e.Graphics.DrawRectangle(
                    SystemPens.Highlight, 
                    this.Bounds);
                TextRenderer.DrawText(
                    e.Graphics,
                    "Terminal",
                    this.font,
                    this.Bounds,
                    SystemColors.Control,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            base.OnPaint(e);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            if (this.DesignMode)
            {
                return;
            }
            
            if (this.terminal != null || this.terminalSubclass != null)
            {
                throw new InvalidOperationException(
                    "Handle has been created already");
            }

            //
            // Create terminal. This loads the native DLL if it's the first time.
            //
            var hr = NativeMethods.CreateTerminal(
                this.Handle, 
                out var terminalHwnd, 
                out this.terminal);
            if (hr != 0)
            {
                throw TerminalException.FromHresult(
                    hr, 
                    "Allocating a terminal failed");
            }

            NativeMethods.TerminalRegisterWriteCallback(
                this.terminal, 
                this.writeCallback);
            NativeMethods.TerminalRegisterScrollCallback(
                this.terminal,
                this.scrollCallback);

            OnThemeChanged();

            //
            // Install a subclassing hook so that we can handle some of the
            // terminal HWND's messages.
            //
            this.terminalSubclass = new SubclassCallback(
                terminalHwnd, 
                this, 
                TerminalSubclassWndProc);
            this.terminalSubclass.UnhandledException += (_, ex) 
                => Application.OnThreadException(ex);
            this.components.Add(this.terminalSubclass.AsComponent());

            //
            // Resize terminal so that it fills the entire control.
            //
            OnResize(EventArgs.Empty);

            if (NativeMethods.GetFocus() == this.terminalSubclass.WindowHandle)
            {
                this.caretBlinkTimer.Start();
            }
            else
            {
                NativeMethods.TerminalSetCursorVisible(this.terminal, false);
            }

            base.OnHandleCreated(e);
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.Width == 0 || this.Height == 0)
            {
                throw new ArgumentException("Size cannot be zero");
            }

            //
            // Notify terminal so that it can adjust dimensions.
            //
            if (this.terminal != null)
            {
                var scrollbarWidth = SystemInformation.VerticalScrollBarWidth;

                var hr = NativeMethods.TerminalTriggerResize(
                    this.terminal,
                    (int)this.Width - scrollbarWidth,
                    (int)this.Height,
                    out var dimensions);
                if (hr != 0)
                {
                    throw TerminalException.FromHresult(
                        hr, 
                        "Adjusting terminal size failed");
                }

                Debug.Assert(dimensions.X <= ushort.MaxValue);
                Debug.Assert(dimensions.Y <= ushort.MaxValue);

                this.Dimensions = new PseudoConsoleSize(
                    (ushort)dimensions.X, 
                    (ushort)dimensions.Y);

                Debug.Assert(this.Dimensions.Width > 0);
                Debug.Assert(this.Dimensions.Height > 0);
            }

            base.OnResize(e);
        }

        protected override void DestroyHandle()
        {
            this.terminalSubclass?.Dispose();
            this.terminal?.Dispose();
            base.DestroyHandle();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (this.DesignMode)
            {
                return;
            }

            Debug.Assert(this.terminalSubclass != null);
            Debug.Assert(this.terminal != null);

            Debug.Assert(this.terminalSubclass != null);
            NativeMethods.SetFocus(this.terminalSubclass!.WindowHandle);

            base.OnGotFocus(e);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            OnThemeChanged();
            base.OnForeColorChanged(e);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            OnThemeChanged();
            base.OnBackColorChanged(e);
        }

        //---------------------------------------------------------------------
        // Terminal subclass.
        //---------------------------------------------------------------------

        private ushort lastKeyUpVirtualKey = 0;
        private string? selectionToClearOnEnter = null;

        private void TerminalSubclassWndProc(ref Message m)
        {
            Debug.Assert(!this.DesignMode);

            var terminalHandle = Invariant.ExpectNotNull(this.terminal, "Terminal");

            var msgId = (WindowMessage)m.Msg;
            switch (msgId)
            {
                case WindowMessage.WM_SETFOCUS:
                    {
                        NativeMethods.TerminalSetFocus(terminalHandle);
                        this.caretBlinkTimer.Start();
                        break;
                    }

                case WindowMessage.WM_KILLFOCUS:
                    {
                        NativeMethods.TerminalKillFocus(terminalHandle);

                        this.caretBlinkTimer.Stop();
                        NativeMethods.TerminalSetCursorVisible(terminalHandle, false);
                        break;
                    }

                case WindowMessage.WM_MOUSEACTIVATE:
                    {
                        Focus();
                        NativeMethods.TerminalSetFocus(terminalHandle);

                        break;
                    }

                case WindowMessage.WM_SYSKEYDOWN:
                case WindowMessage.WM_KEYDOWN:
                    {
                        var keyParams = new WmKeyUpDownParams(m);

                        NativeMethods.TerminalSetCursorVisible(terminalHandle, true);
                        this.caretBlinkTimer.Start();

                        if (keyParams.VirtualKey == (ushort)Keys.Enter && 
                            NativeMethods.TerminalIsSelectionActive(terminalHandle))
                        {
                            //
                            // User pressed enter while a selection was active.
                            // Consistent with the classic Windows console, treat
                            // that as a "copy" command.
                            //
                            // Cache the selected text so that we can process it
                            // in WM_KEYUP.
                            //
                            // NB. We must not pass this key event to the terminal.
                            //
                            this.selectionToClearOnEnter = 
                                NativeMethods.TerminalGetSelection(terminalHandle);
                        }
                        else
                        {
                            NativeMethods.TerminalSendKeyEvent(
                                terminalHandle,
                                keyParams.VirtualKey,
                                keyParams.ScanCode,
                                keyParams.Flags,
                                true);
                        }

                        // TODO: Shift to select (conditional?)
                        // TODO: Ctrl+C/V (conditional)
                        // TODO: Shift/Ctrl+INS (conditional)

                        this.lastKeyUpVirtualKey = keyParams.VirtualKey;
                        break;
                    }

                case WindowMessage.WM_SYSKEYUP:
                case WindowMessage.WM_KEYUP:
                    {
                        var keyParams = new WmKeyUpDownParams(m);

                        if (keyParams.VirtualKey == (ushort)Keys.Enter && 
                            this.selectionToClearOnEnter != null)
                        {
                            //
                            // User pressed enter while a selection was
                            // active. Continue the "copy" behavior begun in
                            // WM_KEYDOWN.
                            //
                            // NB. We must not pass this key event to the
                            // terminal.
                            //
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(this.selectionToClearOnEnter))
                                {
                                    Clipboard.SetText(this.selectionToClearOnEnter);
                                }
                            }
                            catch (ExternalException)
                            {
                                //
                                // Clipboard busy, ignore.
                                //
                            }

                            NativeMethods.TerminalClearSelection(terminalHandle);
                            this.selectionToClearOnEnter = null;
                        }
                        else
                        {
                            if (this.lastKeyUpVirtualKey != keyParams.VirtualKey)
                            {
                                //
                                // For some keys (in particular, TAB and the
                                // arrow keys), we only get a WM_KEYUP and no
                                // preceeding WM_KEYDOWN.
                                // 
                                // When that happens, inject an extra
                                // TerminalSendKeyEvent call so that the
                                // subsequent WM_KEYDOWN isn't ignored by the
                                // terminal.
                                //
                                // NB. We don't know how many WM_KEYDOWNs we
                                // actually missed.
                                //
                                NativeMethods.TerminalSetCursorVisible(
                                    terminalHandle, 
                                    true);
                                NativeMethods.TerminalSendKeyEvent(
                                    terminalHandle,
                                    keyParams.VirtualKey,
                                    keyParams.ScanCode,
                                    keyParams.Flags,
                                    true);
                                this.caretBlinkTimer.Start();
                            }

                            NativeMethods.TerminalSendKeyEvent(
                                terminalHandle,
                                keyParams.VirtualKey,
                                keyParams.ScanCode,
                                keyParams.Flags,
                                false);
                        }
                            
                        break;
                    }

                case WindowMessage.WM_CHAR:
                    {
                        if (this.selectionToClearOnEnter != null)
                        {
                            //
                            // Ignore.
                            //
                        }
                        else
                        { 
                            var charParams = new WmCharParams(m);
                            NativeMethods.TerminalSendCharEvent(
                                terminalHandle,
                                charParams.Character,
                                charParams.ScanCode,
                                charParams.Flags);
                        }

                        break;
                    }


                case WindowMessage.WM_MOUSEWHEEL:
                    {
                        //
                        // The hi-word contains the the distance, in multiples
                        // of 120. 
                        //
                        var delta = (short)(((long)m.WParam) >> 16);
                        if (Control.ModifierKeys.HasFlag(Keys.Control))
                        {
                            //
                            // Control key pressed -> Zoom.
                            //
                            // We only need the sign of the delta to know
                            // whether to zoom in or out.
                            //

                            var oldFont = this.Font;

                            var newFontSize = (delta > 0)
                                ? Math.Min(this.Font.Size + 1, MaximumFontSizeForScrolling)
                                : Math.Max(this.Font.Size - 1, MinimumFontSizeForScrolling);
                            this.Font = new Font(
                                this.Font.FontFamily,
                                newFontSize);

                            oldFont.Dispose();
                        }
                        else
                        {
                            //
                            // Control key not pressed -> scroll.
                            //
                            // Translate delta to the number of lines (+/-) to scroll.
                            //
                            var linesDelta = 
                                delta / 120 * SystemInformation.MouseWheelScrollLines;

                            var currentValue = this.scrollBar.Value;
                            if (linesDelta > 0)
                            {
                                //
                                // Scrolling up.
                                //
                                this.scrollBar.Value = Math.Max(
                                    this.scrollBar.Minimum, 
                                    currentValue - linesDelta);
                            }
                            else
                            {
                                //
                                // Scrolling down.
                                //
                                this.scrollBar.Value = Math.Min(
                                    this.scrollBar.Maximum, 
                                    currentValue - linesDelta);
                            }

                            NativeMethods.TerminalUserScroll(
                                terminalHandle, 
                                this.scrollBar.Value);
                        }
                            
                        break;
                    }

                default:
                    SubclassCallback.DefaultWndProc(ref m);
                    break;
            }
        }

        //---------------------------------------------------------------------
        // Testing-only methods.
        //---------------------------------------------------------------------

        internal void SimulateKey(Keys keyCode)
        {
            Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");

            var subclass = Invariant.ExpectNotNull(
                this.terminalSubclass, 
                "Subclass");

            foreach (var message in KeyboardUtil.ToMessageSequence(
                subclass.WindowHandle, 
                keyCode))
            {
                var m = message;
                TerminalSubclassWndProc(ref m);
            }
        }


        internal void SimulateSend(string data)
        {
            Debug.Assert(!this.InvokeRequired, "Must be called on GUI thread");
            OnUserInput(data);
        }
    }
}
