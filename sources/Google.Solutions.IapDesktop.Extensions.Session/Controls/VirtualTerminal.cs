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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Text;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VtNetCore.VirtualTerminal;
using VtNetCore.VirtualTerminal.Layout;
using VtNetCore.VirtualTerminal.Model;
using VtNetCore.XTermParser;

#nullable disable

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
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
        private readonly VirtualTerminalController controller;
        private readonly DataConsumer controllerSink;
        private readonly VirtualTerminalKeyHandler keyHandler;

        public event EventHandler<SendDataEventArgs> SendData;
        public event EventHandler<TerminalResizeEventArgs> TerminalResized;
        public event EventHandler WindowTitleChanged;

        private TextSelection selection;
        private bool scrolling;
        private TextPosition mouseDownPosition;
        private TerminalFont terminalFont = new TerminalFont();
        private Color caretColor = Color.White;

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public int ViewTop { get; private set; } = 0;

        public int Columns { get; private set; } = -1;

        public int Rows { get; private set; } = -1;

        public string WindowTitle { get; set; }

        public bool EnableCtrlV { get; set; } = true;
        public bool EnableCtrlC { get; set; } = true;
        public bool EnableCtrlA { get; set; } = false;
        public bool EnableShiftInsert { get; set; } = true;
        public bool EnableCtrlInsert { get; set; } = true;
        public bool EnableShiftLeftRight { get; set; } = true;
        public bool EnableShiftUpDown { get; set; } = true;
        public bool EnableTypographicQuoteConversionOnPaste { get; set; } = true;
        public bool EnableCtrlLeftRight { get; set; } = true;
        public bool EnableCtrlUpDown { get; set; } = true;
        public bool EnableCtrlHomeEnd { get; set; } = true;

        internal TerminalFont TerminalFont
        {
            get => this.terminalFont;
            set
            {
                Debug.Assert(value != null);

                var oldFont = this.terminalFont;
                this.terminalFont = value;
                oldFont.Dispose();

                //
                // Recalculate dimensions as the font
                // size might have changed.
                //

                UpdateDimensions();
            }
        }

        internal Color TerminalBackgroundColor
        {
            get => ColorFromTerminalColor(this.controller.BackgroundColor);
            set => this.controller.BackgroundColor = TerminalColorFromColor(value);
        }

        internal Color TerminalForegroundColor
        {
            get => ColorFromTerminalColor(this.controller.ForegroundColor);
            set
            {
                this.controller.ForegroundColor = TerminalColorFromColor(value);
                this.caretColor = value;
            }
        }

        internal Color TerminalCaretColor => this.caretColor;

        public VirtualTerminal()
        {
            InitializeComponent();

            this.controller = new VirtualTerminalController(
                TerminalAttribute.CreateDefault());

            this.controllerSink = new DataConsumer(this.controller);
            this.keyHandler = new VirtualTerminalKeyHandler(this.controller);

            //
            // Forward all logs to common trace source so that they
            // can be controlled using the UI.
            //
            this.controller.Trace.ForwardTo(ApplicationTraceSource.Log);

            //
            // Use double-buffering to reduce flicker - unless we're in an
            // RDP session, cf. https://devblogs.microsoft.com/oldnewthing/20060103-12/?p=32793.
            //
            this.DoubleBuffered = !SystemInformation.TerminalServerSession;

            this.controller.ShowCursor(true);
            this.controller.SendData += (sender, args) =>
            {
                OnSendData(new SendDataEventArgs(Encoding.UTF8.GetString(args.Data)));
            };

            this.Disposed += (sender, args) =>
            {
            };
        }

        private string GetRow(int row, bool pad = false)
        {
            var text = this.controller.GetText(
                0,
                row,
                this.Columns,
                row);

            return pad
                ? text.PadRight(this.Columns, ' ')
                : text;
        }

        //---------------------------------------------------------------------
        // Painting.
        //---------------------------------------------------------------------
        private static Color ColorFromTerminalColor(TerminalColor c)
        {
            return Color.FromArgb((int)c.Red, (int)c.Green, (int)c.Blue);
        }

        private static TerminalColor TerminalColorFromColor(Color c)
        {
            return new TerminalColor(c.R, c.G, c.B);
        }

        private static Color GetSolidColorBrush(string hex)
        {
            byte a = 255;
            var r = (byte)(Convert.ToUInt32(hex.Substring(1, 2), 16));
            var g = (byte)(Convert.ToUInt32(hex.Substring(3, 2), 16));
            var b = (byte)(Convert.ToUInt32(hex.Substring(5, 2), 16));
            return Color.FromArgb(a, r, g, b);
        }

        private TextPosition PositionFromPoint(Point point)
        {
            using (var graphics = CreateGraphics())
            {
                var rowDimensions = this.terminalFont.Measure(graphics, this.Columns);

                var overColumn = (int)Math.Floor(point.X / rowDimensions.Width * this.Columns);
                if (overColumn >= this.Columns)
                {
                    overColumn = this.Columns - 1;
                }

                var overRow = (int)Math.Floor(point.Y / rowDimensions.Height);
                if (overRow >= this.Rows)
                {
                    overRow = this.Rows - 1;
                }

                return new TextPosition(overColumn, overRow);
            }
        }

        private void UpdateDimensions()
        {
            //                
            // Update dimensions.
            //
            using (var graphics = CreateGraphics())
            {
                var columns = this.terminalFont.MeasureColumns(graphics, this.Width);
                var rows = this.terminalFont.MeasureRows(graphics, this.Height);

                if (this.Columns != columns || this.Rows != rows)
                {
                    this.Columns = columns;
                    this.Rows = rows;

                    this.controller.ResizeView(this.Columns, this.Rows);

                    OnTerminalResize(new TerminalResizeEventArgs((ushort)this.Columns, (ushort)this.Rows));
                }
            }

            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (this.Width == 0 && this.Height == 0)
            {
                //
                // Window is probably being minimized. Don't layout
                // and don't update dimensions as that might screw up
                // the screen buffer.
                //
                return;
            }

            UpdateDimensions();
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
            Debug.Assert(this.controller.ViewPort.CursorPosition.Row <= this.Rows);

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
                PaintCaret(e.Graphics, this.CaretPosition);
            }
        }

        private void PaintBackgroundLayer(
            Graphics graphics,
            List<LayoutRow> spans)
        {
            float drawY = 0;
            var rowsPainted = 0;
            foreach (var textRow in spans)
            {
                var rowDimensions = this.terminalFont.Measure(graphics, this.Columns);

                float drawHeight;
                if (rowsPainted + 1 == this.Rows)
                {
                    // Last row. Paint a little further so that we do not leave a gap 
                    // to the bottom.
                    drawHeight = this.Height - drawY;
                }
                else
                {
                    drawHeight = rowDimensions.Height + 1;
                }

                float drawX = 0;
                var columnsPainted = 0;
                foreach (var textSpan in textRow.Spans)
                {
                    var spanDimension = this.terminalFont.Measure(graphics, textSpan.Text.Length);

                    float drawWidth;
                    if (columnsPainted + textSpan.Text.Length == this.Columns)
                    {
                        // This span extends till the end. Paint a little further
                        // so that we do not leave a gap to the right of the last
                        // column.
                        drawWidth = this.Width - drawX;
                    }
                    else
                    {
                        drawWidth = spanDimension.Width;
                    }

                    var bounds = new RectangleF(
                        drawX,
                        drawY,
                        drawWidth,
                        drawHeight);

                    using (var brush = new SolidBrush(GetSolidColorBrush(textSpan.BackgroundColor)))
                    {
                        graphics.FillRectangle(
                            brush,
                            bounds);
                    }

                    drawX += spanDimension.Width;
                    columnsPainted += textSpan.Text.Length;
                }

                drawY += rowDimensions.Height;
                rowsPainted++;
            }
        }

        private void PaintTextLayer(
            Graphics graphics,
            List<LayoutRow> spans)
        {
            float drawY = 0;
            foreach (var textRow in spans)
            {
                float drawX = 0;
                foreach (var textSpan in textRow.Spans)
                {
                    var spanDimension = this.terminalFont.Measure(graphics, textSpan.Text.Length);

                    if (textSpan.Hidden)
                    {
                        drawX += spanDimension.Width;
                        continue;
                    }

                    var fontStyle = textSpan.Bold ? FontStyle.Bold : FontStyle.Regular;
                    if (textSpan.Underline)
                    {
                        fontStyle |= FontStyle.Underline;
                    }

                    this.terminalFont.DrawString(
                        graphics,
                        new PointF(
                            drawX,
                            drawY),
                        textSpan.Text,
                        fontStyle,
                        GetSolidColorBrush(textSpan.ForgroundColor));

                    drawX += spanDimension.Width;
                }

                drawY += this.terminalFont.Measure(graphics, 1).Height;
            }
        }

        private void PaintCaret(Graphics graphics, TextPosition caretPosition)
        {
            var caretY = caretPosition.Row;
            if (caretY < 0 || caretY >= this.Rows)
            {
                // When scrolling, the caret position might escape the viewport.
                // Do not paint a caret if this happens.
                return;
            }

            var caretRect = new Rectangle(
                GetCaretLocation(graphics),
                this.terminalFont.Measure(
                    graphics,
                    1).ToSize());

            using (var brush = new SolidBrush(this.TerminalCaretColor))
            {
                if (this.Focused)
                {
                    // Draw solid box.
                    graphics.FillRectangle(brush, caretRect);
                }
                else
                {
                    // Draw outline.
                    using (var pen = new Pen(brush))
                    {
                        graphics.DrawRectangle(pen, caretRect);
                    }
                }
            }
        }

        private void PaintDiagnostics(Graphics graphics)
        {
#if TERMINAL_DEBUG
            var rowDimensions = this.terminalFont.Measure(graphics, this.Columns);

            var diagnosticText =
                 $"Dimensions: {this.Columns}x{this.Rows}\n" +
                 $"Char size: {rowDimensions.Width / this.Columns}x{rowDimensions.Height}\n" +
                 $"ViewTop: {this.ViewTop}\n" +
                 $"TopRow: {this.controller.ViewPort.TopRow}\n" +
                 $"Screen cursor pos: {this.controller.ViewPort.ScreenCursorPosition}\n" +
                 $"Cursor pos: {this.controller.ViewPort.CursorPosition}\n" +
                 $"Caret pos (viewport): {this.CaretPosition}";

            var size = this.terminalFont.Measure(graphics, diagnosticText);

            this.terminalFont.DrawString(
                graphics,
                new Point(
                    this.Width - (int)size.Width - 5,
                    5),
                diagnosticText,
                FontStyle.Regular,
                Color.Red);
            graphics.DrawRectangle(
                Pens.Red,
                new Rectangle(
                    0,
                    0,
                    (int)Math.Ceiling(rowDimensions.Width),
                    (int)Math.Ceiling(rowDimensions.Height * this.Rows)));
#endif
        }

        //---------------------------------------------------------------------
        // Caret tracking.
        //---------------------------------------------------------------------

        internal TextPosition CaretPosition
        {
            get
            {
                var caretPosition = this.controller.ViewPort.CursorPosition.Clone();
                return caretPosition.OffsetBy(
                    0,
                    this.controller.ViewPort.TopRow - this.ViewTop);
            }
        }

        private Point GetCaretLocation(Graphics graphics)
        {
            var caretPosition = this.CaretPosition;
            if (caretPosition.Column > 0)
            {
                // Make sure the cursor properly aligns with previous text,
                // so use the previous text to determine the position.
                var precedingTextDimensions = this.terminalFont.Measure(
                    graphics,
                    caretPosition.Column);

                return new Point(
                    (int)Math.Ceiling(precedingTextDimensions.Width),
                    (int)Math.Ceiling(caretPosition.Row * precedingTextDimensions.Height));
            }
            else
            {
                // At beginning of line, so there's no text to align with.
                var characterHeight = this.terminalFont.Measure(
                    graphics,
                    1).Height;

                return new Point(
                    0,
                    (int)Math.Ceiling(caretPosition.Row * characterHeight));
            }
        }

        private void InvalidateCaretArea(int chars = 20)
        {
            using (var graphics = CreateGraphics())
            {
                var characterDimensions = this.terminalFont.Measure(graphics, 1).ToSize();
                var caretLocation = GetCaretLocation(graphics);

                // 
                // Invalidate [chars] before and after the caret,
                // and a 10px line above/below.
                //
                var rectAroundCaret = new Rectangle(
                    Math.Max(0, caretLocation.X - chars * characterDimensions.Width),
                    Math.Max(0, caretLocation.Y - 10),
                    2 * chars * characterDimensions.Width,
                    characterDimensions.Height + 20);

                Invalidate(rectAroundCaret);
            }
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

                    if (oldTopRow != this.controller.ViewPort.TopRow && oldTopRow >= this.ViewTop)
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
                        InvalidateCaretArea();
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

            if (!string.IsNullOrWhiteSpace(captured))
            {
                // Copy to clipboard.
                // Trim end since we might be copying empty rows
                // instead (if we have not reached the bottom row
                // yet).
                ClipboardUtil.SetText(captured.TrimEnd());
            }
        }

        internal void PasteClipboard()
        {
            // Paste clipboard.
            var text = ClipboardUtil.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                //
                // Avoid pasting CRLF line endings as causes line
                // endings to be duplicated.
                //
                // Converting CRLF => NL works for most applications,
                // but not for older versions of nano. The correct
                // behavior is to only send a CR.
                //
                text = text.Replace("\r\n", "\r");

                if (this.EnableTypographicQuoteConversionOnPaste)
                {
                    //
                    // Copied code snippets might contain typographic 
                    // quotes (thanks to Word and Docs) - convert them
                    // to plain ASCII single/double quotes.
                    //
                    text = TypographicQuotes.ToAsciiQuotes(text);
                }

                this.controller.Paste(Encoding.UTF8.GetBytes(text));
            }
        }

        private static readonly Keys[] controlKeys = new[]
        {
            Keys.Up,
            Keys.Down,
            Keys.Left,
            Keys.Right,
            Keys.Home,
            Keys.Insert,
            Keys.Delete,
            Keys.End,
            Keys.PageUp,
            Keys.PageDown,
            Keys.F1,
            Keys.F2,
            Keys.F3,
            Keys.F4,
            Keys.F5,
            Keys.F6,
            Keys.F7,
            Keys.F8,
            Keys.F9,
            Keys.F10,
            Keys.F11,
            Keys.F12,
            Keys.Back,
            Keys.Tab,
            Keys.Enter,
            Keys.Escape,
        };

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if ((keyData & Keys.Alt) != 0)
            {
                // Pass to KeyDown.
                return false;
            }

            if (controlKeys.Contains(keyData & ~(Keys.Shift | Keys.Control | Keys.Alt)))
            {
                // Pass all control keys to KeyDown.
                return false;
            }
            else
            {
                return base.ProcessDialogKey(keyData);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (controlKeys.Contains(keyData & ~(Keys.Shift | Keys.Control | Keys.Alt)))
            {
                // Pass all control keys to KeyDown.
                return false;
            }
            else
            {
                // Apply default behavior (i.e. process accelerators).
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private bool ProcessKeyDown(
            Keys keyCode,
            bool control,
            bool alt,
            bool shift)
        {
            this.scrolling = false;

            //
            // Process any non-xterm key sequences.
            //
            if ((this.EnableCtrlV && control && !shift && !alt && keyCode == Keys.V) ||
                (this.EnableShiftInsert && !control && shift && !alt && keyCode == Keys.Insert))
            {
                PasteClipboard();
                return true;
            }
            else if (control && !shift && !alt && keyCode == Keys.C && this.IsTextSelected)
            {
                if (this.EnableCtrlC)
                {
                    CopyClipboard();
                }

                // Clear selection, regardless of whether we copied or not.
                ClearTextSelection();
                return true;
            }
            else if (control && !shift && !alt && keyCode == Keys.Insert && this.IsTextSelected)
            {
                if (this.EnableCtrlInsert)
                {
                    CopyClipboard();
                }

                // Clear selection, regardless of whether we copied or not.
                ClearTextSelection();
                return true;
            }
            else if (this.EnableCtrlA && control && !shift && !alt && keyCode == Keys.A)
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
            else if (this.EnableShiftLeftRight && !control && shift && !alt && keyCode == Keys.Left)
            {
                ExtendSelection(0, -1);
                return true;
            }
            else if (this.EnableShiftLeftRight && !control && shift && !alt && keyCode == Keys.Right)
            {
                ExtendSelection(0, 1);
                return true;
            }
            else if (this.EnableShiftUpDown && !control && shift && !alt && keyCode == Keys.Up)
            {
                ExtendSelection(-1, 0);
                return true;
            }
            else if (this.EnableShiftUpDown && !control && shift && !alt && keyCode == Keys.Down)
            {
                ExtendSelection(1, 0);
                return true;
            }
            else if (this.EnableCtrlLeftRight && control && !shift && !alt && keyCode == Keys.Left)
            {
                // Jump to next word on the left.
                OnSendData(new SendDataEventArgs("\u001b[1;5D"));
                return true;
            }
            else if (this.EnableCtrlLeftRight && control && !shift && !alt && keyCode == Keys.Right)
            {
                // Jump to next word on the right.
                OnSendData(new SendDataEventArgs("\u001b[1;5C"));
                return true;
            }
            else if (this.EnableCtrlUpDown && control && !shift && !alt && keyCode == Keys.Up)
            {
                ScrollViewPort(-1);
                return true;
            }
            else if (this.EnableCtrlUpDown && control && !shift && !alt && keyCode == Keys.Down)
            {
                ScrollViewPort(1);
                return true;
            }
            else if (this.EnableCtrlHomeEnd && control && !shift && !alt && keyCode == Keys.Home)
            {
                ScrollToTop();
                return true;
            }
            else if (this.EnableCtrlHomeEnd && control && !shift && !alt && keyCode == Keys.End)
            {
                ScrollToEnd();
                return true;
            }
            else
            {
                //
                // Check if it is an xterm key sequences. If it is, handle it
                // and skip further key processing by returning true.
                //
                return this.keyHandler.KeyDown(keyCode, alt, control, shift);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = ProcessKeyDown(e.KeyCode, e.Control, e.Alt, e.Shift);

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
            e.Handled = this.keyHandler.KeyPressed(e.KeyChar);

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

        private TextPosition MovePosition(TextPosition position, int rowsDelta, int columnsDelta)
        {
            var currentRowLength = GetRow(position.Row).Length;

            var row = Math.Max(0, Math.Min(position.Row + rowsDelta, this.controller.BottomRow));
            var column = position.Column + columnsDelta;

            if (column < 0)
            {
                if (row == 0)
                {
                    column = 0;
                }
                else
                {
                    // Underflow -> wrap to previous row - but skip any whitespace at end.
                    row--;
                    column += GetRow(row).Length;
                }
            }
            else if (columnsDelta != 0 && column >= currentRowLength)
            {
                // Overflow caused by columnsDelta -> wrap to next row.
                row++;
                column -= currentRowLength;
            }

            return new TextPosition(column, row);
        }

        private void ExtendSelection(int rowsDelta, int columnsDelta)
        {
            if (this.selection == null)
            {
                // Start new selection.
                SelectText(this.CursorPosition, this.CursorPosition, TextSelectionDirection.Forward);
            }

            // Keep the existing direction.
            if (this.selection.Direction == TextSelectionDirection.Forward)
            {
                // Move end of selection.
                SelectText(
                    this.selection.Range.Start,
                    MovePosition(this.selection.Range.End, rowsDelta, columnsDelta),
                    this.selection.Direction);
            }
            else
            {
                // Move start of selection.
                SelectText(
                    MovePosition(this.selection.Range.Start, rowsDelta, columnsDelta),
                    this.selection.Range.End,
                    this.selection.Direction);
            }
        }

        private void SelectWord(TextPosition position)
        {
            var hitChar = GetRow(position.Row, true)[position.Column];

            //
            // Extend selection until we hit a character that is
            // different in whitespace-ness.
            //
            bool predicate(char c) => char.IsWhiteSpace(hitChar) == char.IsWhiteSpace(c);

            SelectText(
                FindPosition(position, predicate, TextSelectionDirection.Backward),
                FindPosition(position, predicate, TextSelectionDirection.Forward),
                TextSelectionDirection.Forward);
        }

        internal void SelectWord(int column, int row)
            => SelectWord(new TextPosition(column, row));

        private TextPosition FindPosition(
            TextPosition startPosition,
            Predicate<char> predicate,
            TextSelectionDirection direction)
        {
            Debug.Assert(
                predicate(GetRow(startPosition.Row, true)[startPosition.Column]),
                "Start position must match predicate");

            if (direction == TextSelectionDirection.Backward)
            {
                //
                // Scan backwards for last character that does not match predicate anymore,
                // then return the position of the last character that does.
                //
                var index = GetRow(startPosition.Row, true)
                    .Substring(0, startPosition.Column)
                    .LastIndexOf(c => !predicate(c));

                Debug.Assert(index < startPosition.Column, "Start position must match predicate");

                if (index >= 0)
                {
                    return new TextPosition(index + 1, startPosition.Row);
                }

                // Scan preceeding rows.
                for (var row = startPosition.Row - 1; row >= 0; row--)
                {
                    index = GetRow(row, true).LastIndexOf(c => !predicate(c));
                    if (index == this.Columns - 1)
                    {
                        return new TextPosition(0, row + 1);
                    }
                    else if (index >= 0)
                    {
                        return new TextPosition(index + 1, row);
                    }
                }

                return new TextPosition(0, 0);
            }
            else
            {
                //
                // Scan forwards for first character that does not match predicate anymore,
                // then return the position of the last character that does.
                //
                var index = GetRow(startPosition.Row, true)
                    .Substring(startPosition.Column)
                    .IndexOf(c => !predicate(c));

                Debug.Assert(index != 0, "Start position must match predicate");

                if (index > 0)
                {
                    return new TextPosition(startPosition.Column + index - 1, startPosition.Row);
                }

                // Scan subsequent rows.
                for (var row = startPosition.Row + 1; row < this.controller.BottomRow; row++)
                {
                    index = GetRow(row, true).IndexOf(c => !predicate(c));
                    if (index == 0)
                    {
                        return new TextPosition(this.Columns - 1, row - 1);
                    }
                    else if (index > 0)
                    {
                        return new TextPosition(index - 1, row);
                    }
                }

                return new TextPosition(this.Columns - 1, this.controller.BottomRow);
            }
        }

        //---------------------------------------------------------------------
        // Scrolling.
        //---------------------------------------------------------------------

        public void ScrollToTop()
        {
            this.ViewTop = 0;
            this.scrolling = true;
            Invalidate();
        }

        public void ScrollToEnd()
        {
            this.ViewTop = this.controller.ViewPort.TopRow;
            this.scrolling = true;
            Invalidate();
        }

        internal void ScrollViewPort(int rowsDelta)
        {
            var oldViewTop = this.ViewTop;

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

        internal void SimulateKey(Keys keyCode, int repeat)
        {
            for (var i = 0; i < repeat; i++)
            {
                SimulateKey(keyCode);
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                this.TerminalFont = (e.Delta >= 0)
                    ? this.terminalFont.NextLargerFont()
                    : this.terminalFont.NextSmallerFont();
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

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SelectWord(PositionFromPoint(e.Location).OffsetBy(0, this.ViewTop));
            }

            base.OnMouseDoubleClick(e);
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
            InvalidateCaretArea();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            InvalidateCaretArea();
            base.OnGotFocus(e);
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
