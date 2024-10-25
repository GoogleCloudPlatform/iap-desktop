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
using Google.Solutions.Common.Text;
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
    /// A virtual terminal based on the Windows Terminal aka "Cascadia".
    /// </summary>
    public partial class VirtualTerminal : UserControl
    {
        private const string DefaultFontFamily = "Consolas";
        private const float DefaultFontSize = 9.75f;
        private const int MinimumFontSizeForScrolling = 6;
        private const int MaximumFontSizeForScrolling = 36;
        private const string Esc = "\u001b";

        private TerminalSafeHandle? terminal = null;
        private IntPtr terminalHwnd;
        private SubclassCallback? terminalSubclass;

        private PseudoTerminalSize dimensions;
        private VirtualTerminalBinding? deviceBinding;

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

        /// <summary>
        /// Create terminal handle. This triggers the native DLL to be loaded.
        /// 
        /// For unit testing, it can be necessary to call this method explicitly.
        /// During normal operation, it's invoked in OnHandleCreated.
        /// </summary>
        internal void CreateTerminalHandle()
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
                out this.terminalHwnd,
                out this.terminal);
            if (hr != 0)
            {
                throw VirtualTerminalException.FromHresult(
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
                this.terminalHwnd,
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
        public PseudoTerminalSize Dimensions
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
        public IPseudoTerminal? Device
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
                    this.deviceBinding = new VirtualTerminalBinding(
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
                Invoke(((Action)(() => OnOutputReceived(data))));
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
                Invoke(((Action)(() => OnDeviceError(e))));
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
                Invoke(((Action)(() => OnDeviceClosed())));
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
        public event EventHandler<VirtualTerminalOutputEventArgs>? Output;

        /// <summary>
        /// Terminal received user input that needs to be sent to the device.
        /// </summary>
        public event EventHandler<VirtualTerminalInputEventArgs>? UserInput;

        /// <summary>
        /// The device has failed.
        /// </summary>
        public event EventHandler<VirtualTerminalErrorEventArgs>? DeviceError;

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

        /// <summary>
        /// Invoked when the terminal received user input that needs to
        /// be sent to the device.
        /// </summary>
        protected virtual void OnUserInput(string data)
        {
            //
            // When a user presses Enter, the terminal produces a CR (\r).
            // This is in line with what you'd expect.
            //
            // If the user pastes text into the terminal (for example, by
            // using a right-click, which is handled by the terminal itself),
            // and the pasted text contains CRLFs, the the terminal doesn't
            // covert these CRLFs to CRs. Instead, it passes through the CRLFs
            // to this callback here.
            //
            // Sanitize the CRLFs here, because most applications (incl. bash)
            // will otherwise interpret a CRLF as two line breaks.
            //
            data = data.Replace("\r\n", "\r");

            this.UserInput?.Invoke(this, new VirtualTerminalInputEventArgs(data));
        }

        /// <summary>
        /// Invoked when the device produced output that needs to be
        /// sent to the terminal for rendering.
        /// </summary>
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

            this.Output?.Invoke(this, new VirtualTerminalOutputEventArgs(data));
        }

        protected virtual void OnDeviceError(Exception e)
        {
            this.DeviceError?.Invoke(this, new VirtualTerminalErrorEventArgs(e));
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

            NativeMethods.TerminalUserScroll(
                Invariant.ExpectNotNull(this.terminal, "Terminal"), 
                e.NewValue);
        }

        private void OnScrollbarValueChanged(object sender, System.EventArgs e)
        {
            if (this.DesignMode)
            {
                return;
            }

            NativeMethods.TerminalUserScroll(
                Invariant.ExpectNotNull(this.terminal, "Terminal"),
                this.scrollBar.Value);
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
                //
                // Draw a placeholder where the terminal would appear.
                //
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
            CreateTerminalHandle();
            base.OnHandleCreated(e);
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.Width == 0 || this.Height == 0)
            {
                //
                // This happens when the window is being minimized.
                // We can ignore that.
                //
                return;
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
                    throw VirtualTerminalException.FromHresult(
                        hr,
                        "Adjusting terminal size failed");
                }

                Debug.Assert(dimensions.X <= ushort.MaxValue);
                Debug.Assert(dimensions.Y <= ushort.MaxValue);

                this.Dimensions = new PseudoTerminalSize(
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

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                //
                // Process TAB as a normal input key instead of moving
                // the focus away from this control.
                //
                e.IsInputKey = true;
            }

            base.OnPreviewKeyDown(e);
        }

        //---------------------------------------------------------------------
        // Terminal subclass.
        //---------------------------------------------------------------------

        private bool ignoreWmCharBecauseOfAccelerator = false;
        private ushort lastKeyDownVirtualKey = 0;
        private string? selectionToCopyInKeyUp = null;

        private void TerminalSubclassWndProc(ref Message m)
        {
            bool IsAcceleratorForCopyingCurrentSelection(Keys key)
            {
                if (ModifierKeys == Keys.None && key == Keys.Enter)
                {
                    //
                    // Consistent with the classic Windows console, treat
                    // Enter as a "copy" command.
                    //
                    return true;
                }
                else if (ModifierKeys == Keys.Control && key == Keys.Insert)
                {
                    return this.EnableCtrlInsert;
                }
                else if (ModifierKeys == Keys.Control && key == Keys.C)
                {
                    //
                    // NB. Powershell handles Ctrl+C itself, but cmd and bash
                    // don't.
                    //
                    return this.EnableCtrlC;
                }
                else
                {
                    return false;
                };
            }

            bool IsAcceleratorForPasting(Keys key)
            {
                if (ModifierKeys == Keys.Shift && key == Keys.Insert)
                {
                    return this.EnableShiftInsert;
                }
                else if (ModifierKeys == Keys.Control && key == Keys.V)
                {
                    return this.EnableCtrlV;
                }
                else
                {
                    return false;
                };
            }

            bool IsAcceleratorForScrollingToTop(Keys key)
            {
                if (ModifierKeys == Keys.Control && key == Keys.Home)
                {
                    return this.EnableCtrlHome;
                }
                else
                {
                    return false;
                };
            }

            bool IsAcceleratorForScrollingToBottom(Keys key)
            {
                if (ModifierKeys == Keys.Control && key == Keys.Home)
                {
                    return this.EnableCtrlEnd;
                }
                else
                {
                    return false;
                };
            }

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

                        if (IsAcceleratorForCopyingCurrentSelection((Keys)keyParams.VirtualKey) &&
                            NativeMethods.TerminalIsSelectionActive(terminalHandle))
                        {
                            //
                            // Begin "copy" operation.
                            //
                            // Cache the selected text so that we can process it
                            // in WM_KEYUP.
                            //
                            // NB. We must not pass this key event to the terminal.
                            //
                            this.ignoreWmCharBecauseOfAccelerator = true;
                            this.selectionToCopyInKeyUp =
                                NativeMethods.TerminalGetSelection(terminalHandle);
                        }
                        else if (IsAcceleratorForPasting((Keys)keyParams.VirtualKey))
                        {
                            //
                            // We'll handle the paste in WM_KEYUP.
                            //
                            // NB. We must not pass this key event to the terminal.
                            //
                            this.ignoreWmCharBecauseOfAccelerator = true;
                        }
                        else if (IsAcceleratorForScrollingToTop((Keys)keyParams.VirtualKey))
                        {
                            this.scrollBar.Value = 0;
                        }
                        else if (IsAcceleratorForScrollingToBottom((Keys)keyParams.VirtualKey))
                        {
                            this.scrollBar.Value = this.scrollBar.Maximum;
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

                        this.lastKeyDownVirtualKey = keyParams.VirtualKey;
                        break;
                    }

                case WindowMessage.WM_SYSKEYUP:
                case WindowMessage.WM_KEYUP:
                    {
                        var keyParams = new WmKeyUpDownParams(m);

                        if (IsAcceleratorForCopyingCurrentSelection((Keys)keyParams.VirtualKey) &&
                            this.selectionToCopyInKeyUp != null)
                        {
                            Debug.Assert(this.ignoreWmCharBecauseOfAccelerator);

                            //
                            // Continue the "copy" operation begun in WM_KEYDOWN.
                            //
                            // NB. We must not pass this key event to the
                            // terminal.
                            //
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(this.selectionToCopyInKeyUp))
                                {
                                    Clipboard.SetText(this.selectionToCopyInKeyUp);
                                }
                            }
                            catch (ExternalException)
                            {
                                //
                                // Clipboard busy, ignore.
                                //
                            }

                            NativeMethods.TerminalClearSelection(terminalHandle);
                            this.selectionToCopyInKeyUp = null;
                            this.ignoreWmCharBecauseOfAccelerator = false;
                        }
                        else if (IsAcceleratorForPasting((Keys)keyParams.VirtualKey))
                        {
                            Debug.Assert(this.ignoreWmCharBecauseOfAccelerator);

                            try
                            {
                                var contents = Clipboard.GetText();
                                if (!string.IsNullOrWhiteSpace(contents))
                                {
                                    if (this.EnableTypographicQuoteConversion)
                                    {
                                        contents = TypographicQuotes.ToAsciiQuotes(contents);
                                    }

                                    OnUserInput(contents);
                                }
                            }
                            catch (ExternalException)
                            {
                                //
                                // Clipboard busy, ignore.
                                //
                            }

                            this.ignoreWmCharBecauseOfAccelerator = false;
                        }
                        else
                        {
                            if (this.lastKeyDownVirtualKey != keyParams.VirtualKey)
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
                        if (this.ignoreWmCharBecauseOfAccelerator)
                        {
                            //
                            // Ignore these keys because they're part of an accelerator.
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

        internal bool TerminalHandleCreated
        {
            get => this.terminal != null;
        }

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
