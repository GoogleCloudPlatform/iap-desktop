//
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using VtNetCore.VirtualTerminal;
using VtNetCore.XTermParser;
using VtNetCore.VirtualTerminal.Layout;
using Google.Solutions.Common.Diagnostics;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Controls
{
    /// <summary>
    /// Virtual terminal control.
    /// 
    /// 
    ///    GUI        +----------+                       +--------+
    ///   output  <-- |          | <===(ReceiveData)==== |        |
    ///               | Terminal |                       | Server |
    ///  Keyboard --> |          | ==(SendData event)==> |        | 
    ///   events      +----------+                       +--------+
    /// 
    /// 
    /// </summary>
    [SkipCodeCoverage("UI code")]
    public partial class VirtualTerminal : UserControl
    {
        private readonly Point TextOrigin = new Point(3, 0);
        private readonly VirtualTerminalController controller;
        private readonly DataConsumer controllerSink;

        public event EventHandler<SendDataEventArgs> SendData;
        public event EventHandler<TerminalResizeEventArgs> TerminalResized;
        public event EventHandler WindowTitleChanged;

        #pragma warning disable IDE0069 // Disposable fields should be disposed
        private Caret caret;
        #pragma warning restore IDE0069 // Disposable fields should be disposed

        private TextSelection selection;
        private bool scrolling;
        private TextPosition mouseDownPosition;

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public int ViewTop { get; set; } = 0;

        public int Columns { get; private set; } = -1;

        public int Rows { get; private set; } = -1;

        public string WindowTitle { get; set; }

        public bool EnableCtrlV { get; set; } = true;
        public bool EnableCtrlC { get; set; } = true;
        public bool EnableCtrlA { get; set; } = true;
        public bool EnableShiftInsert { get; set; } = true;
        public bool EnableCtrlInsert { get; set; } = true;

        public VirtualTerminal()
        {
            InitializeComponent();

            this.controller = new VirtualTerminalController();
            this.controllerSink = new DataConsumer(this.controller);

            //
            // Use double-buffering to reduce flicker - unless we're in an
            // RDP session, cf. https://devblogs.microsoft.com/oldnewthing/20060103-12/?p=32793.
            //
            this.DoubleBuffered = !SystemInformation.TerminalServerSession;

            this.Font = new Font(TerminalFont.FontFamily, 9.75f);

            this.controller.ShowCursor(true);
            this.controller.SendData += (sender, args) =>
            {
                OnSendData(new SendDataEventArgs(Encoding.UTF8.GetString(args.Data)));
            };

            this.Disposed += (sender, args) =>
            {
                this.caret?.Dispose();
            };
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                if (!TerminalFont.IsValidFont(value))
                {
                    throw new ArgumentException("Unsuitable font");
                }

                base.Font = value;
            }
        }

        //---------------------------------------------------------------------
        // Painting.
        //---------------------------------------------------------------------

        private Caret GetCaret()
        {
            if (this.caret == null)
            {
                this.caret = new Caret(this, this.CharacterSize.ToSize());
            }

            return this.caret;
        }

        private static Color GetSolidColorBrush(string hex)
        {
            byte a = 255;
            byte r = (byte)(Convert.ToUInt32(hex.Substring(1, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(3, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(5, 2), 16));
            return Color.FromArgb(a, r, g, b);
        }

        private SizeF CharacterSize => TerminalFont.GetCharacterSize(this.Font);

        private TextPosition PositionFromPoint(Point point)
        {
            var characterSize = this.CharacterSize;

            int overColumn = (int)Math.Floor(point.X / characterSize.Width);
            if (overColumn >= this.Columns)
            {
                overColumn = this.Columns - 1;
            }

            int overRow = (int)Math.Floor(point.Y / characterSize.Height);
            if (overRow >= this.Rows)
            {
                overRow = this.Rows - 1;
            }

            return new TextPosition(overColumn, overRow);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            //                
            // Update dimensions.
            //
            var characterSize = this.CharacterSize;

            int columns = (int)Math.Floor(this.Width / characterSize.Width);
            int rows = (int)Math.Floor(this.Height / characterSize.Height);
            if (this.Columns != columns || this.Rows != rows)
            {
                this.Columns = columns;
                this.Rows = rows;

                this.controller.ResizeView(this.Columns, this.Rows);

                OnTerminalResize(new TerminalResizeEventArgs((ushort)this.Columns, (ushort)this.Rows));
            }

            base.OnLayout(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.DesignMode)
            {
                return;
            }

            Debug.Assert(this.Rows > 0);
            Debug.Assert(this.Columns > 0);
            Debug.Assert(this.controller.ViewPort.CursorPosition.Row <= Rows);

            var terminalTop = this.controller.ViewPort.TopRow;
            var spans = this.controller.ViewPort.GetPageSpans(
                this.ViewTop,
                this.Rows,
                this.Columns,
                this.selection?.Range);

            if (!this.scrolling && this.ViewTop != terminalTop)
            {
                this.ViewTop = terminalTop;
            }

            PaintBackgroundLayer(e.Graphics, spans);
            PaintTextLayer(e.Graphics, spans);
            PaintDiagnostics(e.Graphics);

            if (this.controller.CursorState.ShowCursor)
            {
                var caretPosition = this.controller.ViewPort.CursorPosition.Clone();
                PaintCaret(caretPosition.OffsetBy(0, terminalTop - this.ViewTop));
            }
        }

        private void PaintBackgroundLayer(
            Graphics graphics,
            List<LayoutRow> spans)
        {
            var characterSize = this.CharacterSize;

            double drawY = 0;
            int rowsPainted = 0;
            foreach (var textRow in spans)
            {
                int drawHeight;
                if (rowsPainted + 1 == this.Rows)
                {
                    // Last row. Paint a little further so that we do not leave a gap 
                    // to the bottom.
                    drawHeight = this.Height - (int)drawY;
                }
                else
                {
                    drawHeight = (int)(characterSize.Height + 1);
                }

                double drawX = 0;
                int columnsPainted = 0;
                foreach (var textSpan in textRow.Spans)
                {
                    int drawWidth;
                    if (columnsPainted + textSpan.Text.Length == this.Columns)
                    {
                        // This span extends till the end. Paint a little further
                        // so that we do not leave a gap to the right of the last
                        // column.
                        drawWidth = this.Width - (int)drawX;
                    }
                    else
                    {
                        drawWidth = (int)(characterSize.Width * (textSpan.Text.Length) + 1);
                    }

                    var bounds = new Rectangle(
                        (int)drawX,
                        (int)drawY,
                        drawWidth,
                        drawHeight);

                    using (var brush = new SolidBrush(GetSolidColorBrush(textSpan.BackgroundColor)))
                    {
                        graphics.FillRectangle(
                            brush,
                            bounds);
                    }

                    drawX += characterSize.Width * (textSpan.Text.Length);
                    columnsPainted += textSpan.Text.Length;
                }

                drawY += characterSize.Height;
                rowsPainted++;
            }
        }

        private void PaintTextLayer(
            Graphics graphics,
            List<LayoutRow> spans)
        {
            double drawY = 0;
            foreach (var textRow in spans)
            {
                var characterSize = this.CharacterSize;

                double drawX = 0;
                foreach (var textSpan in textRow.Spans)
                {
                    var drawWidth = characterSize.Width * (textSpan.Text.Length);

                    if (textSpan.Hidden)
                    {
                        drawX += drawWidth;
                        continue;
                    }

                    var fontStyle = textSpan.Bold ? FontStyle.Bold : FontStyle.Regular;
                    if (textSpan.Underline)
                    {
                        fontStyle |= FontStyle.Underline;
                    }

                    using (var brush = new SolidBrush(GetSolidColorBrush(textSpan.ForgroundColor)))
                    using (var font = new Font(this.Font, fontStyle))
                    {
                        graphics.DrawString(
                            textSpan.Text,
                            font,
                            brush,
                            new PointF(
                                (float)drawX,
                                (float)drawY));
                    }

                    drawX += characterSize.Width * (textSpan.Text.Length);
                }

                drawY += characterSize.Height;
            }
        }

        private void PaintCaret(TextPosition caretPosition)
        {
            var caretY = caretPosition.Row;
            if (caretY < 0 || caretY >= Rows)
            {
                return;
            }

            var characterSize = this.CharacterSize;
            var drawX = (int)(caretPosition.Column * characterSize.Width);
            var drawY = (int)(caretY * characterSize.Height);

            GetCaret().Position = new Point(this.TextOrigin.X + drawX, this.TextOrigin.Y + drawY);
        }

        private void PaintDiagnostics(Graphics graphics)
        {
#if DEBUG
            var characterSize = this.CharacterSize;
            var diagnosticText =
                 $"Dimensions: {this.Columns}x{this.Rows}\n" +
                 $"Char size: {characterSize.Width}x{characterSize.Height}\n" +
                 $"ViewTop : {this.ViewTop}\n" +
                 $"Cursor pos: {this.controller.ViewPort.CursorPosition}\n" +
                 $"Screen cursor pos: {this.controller.ViewPort.ScreenCursorPosition}";

            using (var font = new Font(this.Font.FontFamily, 8))
            {
                var size = graphics.MeasureString(diagnosticText, font);

                graphics.DrawString(
                    diagnosticText,
                    font,
                    Brushes.Red,
                    new PointF(
                        this.Width - size.Width - 5,
                        5));
                graphics.DrawRectangle(
                    Pens.Red,
                    new Rectangle(
                        0,
                        0,
                        (int)(this.Columns * characterSize.Width),
                        (int)(this.Rows * characterSize.Height)));
            }
#endif
        }

        //---------------------------------------------------------------------
        // Actions.
        //---------------------------------------------------------------------

        public void ReceiveData(string text)
        {
            lock (this.controller)
            {
                var oldTopRow = this.controller.ViewPort.TopRow;
                var oldViewTop = this.ViewTop;
                var oldCursorRow = this.controller.ViewPort.CursorPosition.Row;

                this.controllerSink.Push(Encoding.UTF8.GetBytes(text));

                if (this.controller.Changed)
                {
                    var changeCount = this.controller.ChangeCount;
                    this.controller.ClearChanges();

                    if (oldTopRow != this.controller.ViewPort.TopRow && oldTopRow >= ViewTop)
                    {
                        this.ViewTop = this.controller.ViewPort.TopRow;
                    }

                    if (changeCount == 1 &&
                        this.ViewTop == oldViewTop &&
                        this.controller.ViewPort.CursorPosition.Row == oldCursorRow)
                    {
                        //
                        // Single-character change that did not cause a line to wrap.
                        // Avoid a full redraw and instead only redraw a small region
                        // around the cursor.
                        //
                        // NB. This optimization has a significant effect when the
                        // application is running in an RDP session, but the effect
                        // is negligble otherwise.
                        //
                        var caretPos = GetCaret().Position;
                        Invalidate(new Rectangle(
                            0,
                            caretPos.Y,
                            this.Width,
                            Math.Min(100, this.Height - caretPos.Y)));
                    }
                    else
                    {
                        Invalidate();
                    }
                }
            }
        }

        //---------------------------------------------------------------------
        // Keyboard event handlers.
        //---------------------------------------------------------------------

        private void CopyClipboard()
        {
            var captured = this.TextSelection;

            if (!string.IsNullOrEmpty(captured))
            {
                // Copy to clipboard.
                // Trim end since we might be copying empty rows
                // instead (if we have not reached the bottom row
                // yet).
                Clipboard.SetText(captured.TrimEnd());
            }
        }

        internal void PasteClipboard()
        {
            // Paste clipboard.
            var text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                //
                // Convert to Unix line endings, otherwise pasting a multi-
                // line command will be interpreted as a sequence of
                // commands.
                //
                text = text.Replace("\r\n", "\n");
                this.controller.Paste(Encoding.UTF8.GetBytes(text));
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.Alt) != 0)
            {
                // Pass to KeyDown.
                return false;
            }

            switch (keyData)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.Insert:
                case Keys.Delete:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.F1:
                case Keys.F2:
                case Keys.F3:
                case Keys.F4:
                case Keys.F5:
                case Keys.F6:
                case Keys.F7:
                case Keys.F8:
                case Keys.F9:
                case Keys.F10:
                case Keys.F11:
                case Keys.F12:
                case Keys.Back:
                case Keys.Tab:
                case Keys.Enter:
                case Keys.Escape:
                    // Pass all control keys to KeyDown.
                    return false;

                default:
                    return base.ProcessDialogKey(keyData);
            }
        }

        private bool IsKeySequence(string key, bool control, bool shift)
        {
            return this.controller.GetKeySequence(key, control, shift) != null;
        }

        private static string NameFromKey(Keys key)
        {
            // Return name that is compatible with vtnetcore's KeyboardTranslation
            switch (key)
            {
                case Keys.Next: // Alias for PageDown
                    return "PageDown";

                case Keys.Prior:   // Alias for PageUp
                    return "PageUp";

                default:
                    return key.ToString();
            }
        }

        private bool SendKey(
            Keys keyCode,
            bool control,
            bool alt,
            bool shift)
        {
            this.scrolling = false;

            if ((this.EnableCtrlV && control && !shift && keyCode == Keys.V) ||
                (this.EnableShiftInsert && !control && shift && keyCode == Keys.Insert))
            {
                PasteClipboard();
                return true;
            }
            else if (control && !shift && keyCode == Keys.C && this.IsTextSelected)
            {
                if (this.EnableCtrlC)
                {
                    CopyClipboard();
                }

                // Clear selection, regardless of whether we copied or not.
                ClearTextSelection();
                return true;
            }
            else if (control && !shift && keyCode == Keys.Insert && this.IsTextSelected)
            {
                if (this.EnableCtrlInsert)
                {
                    CopyClipboard();
                }

                // Clear selection, regardless of whether we copied or not.
                ClearTextSelection();
                return true;
            }
            else if (this.EnableCtrlA && control && !shift && keyCode == Keys.A)
            {
                SelectAllText();
                return true;
            }
            else if (keyCode == Keys.Enter && this.IsTextSelected)
            {
                // Just clear selection, but do not send the key.
                ClearTextSelection();
                return true;
            }
            else if (!alt && IsKeySequence(
                NameFromKey(keyCode),
                control,
                shift))
            {
                //
                // This is a key sequence that needs to be
                // translated to some VT sequence.
                //
                // NB. If Alt is pressed, it cannot be a key sequence. 
                // Otherwise, it might.
                //
                return this.controller.KeyPressed(
                    NameFromKey(keyCode),
                    control,
                    shift);
            }
            else if (alt && control)
            {
                //
                // AltGr - let KeyPress handle the composition.
                //
                return false;
            }
            else if (alt)
            {
                //
                // Somewhat non-standard, emulate the behavior
                // of other terminals and escape the character.
                //
                // This enables applications like midnight 
                // commander which rely on Alt+<char> keyboard
                // shortcuts.
                //
                var ch = KeyUtil.CharFromKeyCode(keyCode);
                if (ch.Length > 0)
                {
                    OnSendData(new SendDataEventArgs("\u001b" + ch));
                    return true;
                }
                else
                {
                    //
                    // This is a stray Alt press, could be part
                    // of an Alt+Tab action. Do not handle this
                    // as it might screw up subsequent input.
                    //
                    return false;
                }
            }
            else
            {
                //
                // This is a plain character. Defer handling to 
                // KeyPress so that Windows does the nasty key
                // composition and dead key handling for us.
                //
                return false;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = SendKey(e.KeyCode, e.Control, e.Alt, e.Shift);

            //
            // Suppress KeyPress if we already handled the key.
            //
            // This also ensures that KeyPress is never called
            // when an Alt + ... is pressed - that's important
            // because otherwise the event bubbles up and moves
            // the focus to the menu strip (if any).
            //
            e.SuppressKeyPress = e.Handled;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            //
            // This character is guaranteed to not be part of
            // a key sequence, because OnKeyDown handles those
            //
            // That means the status of the Control, Alt, and
            // Shift modifiers also does not matter.
            //
            e.Handled = this.controller.KeyPressed(
                e.KeyChar.ToString(),
                false,
                false);

            ClearTextSelection();
        }

        protected override void OnResize(EventArgs e)
        {
            Invalidate();
            base.OnResize(e);
        }

        //---------------------------------------------------------------------
        // Text selection tracking.
        //---------------------------------------------------------------------

        private TextPosition CursorPosition
        {
            // Return absolute cursor position (i.e., ignore view port)
            get => new TextPosition(
                this.controller.CursorState.Position.Column,
                this.controller.CursorState.Position.Row + this.ViewTop);
        }

        private void SelectAllText()
        {
            this.selection = new TextSelection(
                new TextRange()
                {
                    Start = new TextPosition(0, 0),
                    End = new TextPosition(this.Columns, this.controller.BottomRow)
                },
                TextSelectionDirection.Forward);

            Invalidate();
        }

        internal bool IsTextSelected => this.selection != null;

        internal string TextSelection
        {
            get
            {
                if (this.selection == null)
                {
                    return null;
                }
                else
                {
                    //
                    // NB. If the mouse escapes the window borders, the coordinates
                    // can get negative - therefore, use the max.
                    //
                    return this.controller.GetText(
                        Math.Max(0, this.selection.Range.Start.Column),
                        Math.Max(0, this.selection.Range.Start.Row),
                        Math.Max(0, this.selection.Range.End.Column),
                        Math.Max(0, this.selection.Range.End.Row));
                }
            }
        }

        internal void ClearTextSelection()
        {
            if (this.selection != null)
            {
                // Clear selection.
                this.selection = null;
                Invalidate();
            }
        }

        internal void SelectText(
            TextPosition start,
            TextPosition end,
            TextSelectionDirection direction)
        {
            this.selection = new TextSelection(
                new TextRange()
                {
                    Start = start,
                    End = end
                },
                direction);
            Invalidate();
        }

        internal void SelectText(
            ushort startColumn,
            ushort startRow,
            ushort endColumn,
            ushort endRow,
            TextSelectionDirection direction)
            => SelectText(
                new TextPosition(startColumn, startRow),
                new TextPosition(endColumn, endRow),
                direction);

        //private void ExtendSelectionByOneCharacter(TextSelectionDirection direction)
        //{
        //    if (this.selection != null)
        //    {
        //        // Extend current selection.
        //    }
        //    else
        //    {
        //        // Start new selection.
        //        if (direction == TextSelectionDirection.Forward)
        //        {
        //            //SelectText(this.CursorPosition, this.)
        //        }
        //        else
        //        {

        //        }
        //    }
        //}

        //private void ExtendSelectionTillEnd(TextSelectionDirection direction)
        //{
        //    if (direction == TextSelectionDirection.Forward)
        //    {
        //        SelectText(
        //            this.selection?.Range.Start ?? this.CursorPosition,
        //            )
        //    }
        //    else
        //    {

        //    }
        //}

        //---------------------------------------------------------------------
        // For testing only.
        //---------------------------------------------------------------------

        internal void MoveCursorRelative(int x, int y)
        {
            this.controller.MoveCursorRelative(x, y);
        }

        internal void SimulateKey(Keys keyCode)
        {
            var keyDown = new KeyEventArgs(keyCode);
            OnKeyDown(keyDown);
            if (!keyDown.SuppressKeyPress)
            {
                // NB. This does not work for any combining characters, but
                // that's ok since this method is for testing only.
                var ch = KeyUtil.CharFromKeyCode(keyCode);
                if (ch.Length >= 1)
                {
                    OnKeyPress(new KeyPressEventArgs(ch[0]));
                }
            }
        }

        internal string GetBuffer()
        {
            var buffer = new StringBuilder();

            var spans = this.controller.ViewPort.GetPageSpans(
                this.ViewTop,
                this.Rows,
                this.Columns,
                this.selection?.Range);

            foreach (var textRow in spans)
            {
                foreach (var textSpan in textRow.Spans)
                {
                    if (!textSpan.Hidden)
                    {
                        buffer.Append(textSpan.Text);
                    }
                }

                buffer.Append("\r\n");
            }

            return buffer.ToString();
        }

        //---------------------------------------------------------------------
        // Mouse event handlers.
        //---------------------------------------------------------------------

        internal void ScrollViewPort(int rowsDelta)
        {
            int oldViewTop = this.ViewTop;

            this.ViewTop += rowsDelta;
            this.scrolling = true;

            if (this.ViewTop < 0)
            {
                this.ViewTop = 0;
            }
            else if (this.ViewTop > this.controller.ViewPort.TopRow)
            {
                this.ViewTop = this.controller.ViewPort.TopRow;
            }

            if (oldViewTop != this.ViewTop)
            {
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Zoom.
                Font newFont = (e.Delta >= 0)
                    ? TerminalFont.NextLargerFont(this.Font)
                    : TerminalFont.NextSmallerFont(this.Font);

                var oldFont = this.Font;
                if (newFont != oldFont)
                {
                    this.Font = newFont;
                    oldFont.Dispose();

                    // Force new caret so that it uses the new font size too.
                    this.GetCaret()?.Dispose();
                    this.caret = null;

                    Invalidate();
                }
            }
            else
            {
                ScrollViewPort(-e.Delta / 40);
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                PasteClipboard();
            }
            else if (e.Button == MouseButtons.Left)
            {
                // Begin a new selection. Memorize the location so that we
                // can start tracking.
                this.selection = null;
                this.mouseDownPosition = PositionFromPoint(e.Location)
                    .OffsetBy(0, this.ViewTop);

                // Repaint to show selection.
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.mouseDownPosition != null)
            {
                // We're tracking a selection.
                var textPosition = PositionFromPoint(e.Location).OffsetBy(0, this.ViewTop);

                if (this.mouseDownPosition != textPosition)
                {
                    if (this.mouseDownPosition <= textPosition)
                    {
                        this.selection = new TextSelection(
                            new TextRange
                            {
                                Start = this.mouseDownPosition,
                                End = textPosition.OffsetBy(-1, 0)
                            },
                            TextSelectionDirection.Forward);
                    }
                    else
                    {
                        this.selection = new TextSelection(
                            new TextRange
                            {
                                Start = textPosition,
                                End = this.mouseDownPosition
                            },
                            TextSelectionDirection.Backward);
                    }

                    // Repaint to show selection.
                    Invalidate();
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.mouseDownPosition != null && this.selection != null)
            {
                // We're tracking a selection.
                CopyClipboard();

                return;
            }

            base.OnMouseUp(e);
        }

        //---------------------------------------------------------------------
        // Other event handlers.
        //---------------------------------------------------------------------

        protected override void OnBackColorChanged(EventArgs e)
        {
            this.controller.SetRgbBackgroundColor(
                this.BackColor.R,
                this.BackColor.G,
                this.BackColor.B);
            base.OnBackColorChanged(e);
        }

        //protected override void OnForeColorChanged(EventArgs e)
        //{
        //    this.controller.SetRgbForegroundColor(
        //        this.ForeColor.R,
        //        this.ForeColor.G,
        //        this.ForeColor.B);
        //    base.OnForeColorChanged(e);
        //}

        protected override void OnLostFocus(EventArgs e)
        {
            GetCaret()?.Dispose();
            this.caret = null;

            base.OnLostFocus(e);
        }

        //---------------------------------------------------------------------
        // Event handlers.
        //---------------------------------------------------------------------

        protected virtual void OnWindowTitleChanged(TextEventArgs e)
        {
            this.WindowTitle = e.Text;
            this.WindowTitleChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSendData(SendDataEventArgs args)
        {
            this.SendData?.Invoke(this, args);
        }

        protected virtual void OnTerminalResize(TerminalResizeEventArgs args)
        {
            this.TerminalResized?.Invoke(this, args);
        }
    }

    public class TextSelection
    {
        public TextRange Range { get; }
        public TextSelectionDirection Direction { get; }

        public TextSelection(
            TextRange range,
            TextSelectionDirection direction)
        {
            this.Range = range;
            this.Direction = direction;
        }
    }

    public enum TextSelectionDirection
    {
        Forward,
        Backward
    }

    public class SendDataEventArgs : EventArgs
    {
        public string Data { get; }

        public SendDataEventArgs(string data)
        {
            this.Data = data;
        }
    }

    public class TerminalResizeEventArgs : EventArgs
    {
        public ushort Columns { get; }
        public ushort Rows { get; }

        public TerminalResizeEventArgs(ushort columns, ushort rows)
        {
            this.Columns = columns;
            this.Rows = rows;
        }
    }
}
