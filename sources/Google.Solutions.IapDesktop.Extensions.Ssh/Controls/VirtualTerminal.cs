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

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Controls
{
    /// <summary>
    /// Virtual terminal control.
    /// </summary>
    [SkipCodeCoverage("UI code")]
    public partial class VirtualTerminal : UserControl
    {
        private readonly Point TextOrigin = new Point(3, 0);
        private readonly VirtualTerminalController controller;
        private readonly DataConsumer controllerSink;

        public event EventHandler<InputEventArgs> InputReceived;
        public event EventHandler<TerminalResizeEventArgs> TerminalResized;
        public event EventHandler WindowTitleChanged;

        #pragma warning disable IDE0069 // Disposable fields should be disposed
        private Caret caret;
        #pragma warning restore IDE0069 // Disposable fields should be disposed

        private TextRange textSelection;
        private bool scrolling;
        private TextPosition mouseDownPosition;

        //---------------------------------------------------------------------
        // Properties.
        //---------------------------------------------------------------------

        public int ViewTop { get; set; } = 0;

        public int Columns { get; private set; } = -1;

        public int Rows { get; private set; } = -1;

        public int BottomRow => ViewTop + Rows - 1;

        public string WindowTitle { get; set; }

        public VirtualTerminal()
        {
            InitializeComponent();

            this.controller = new VirtualTerminalController();
            this.controllerSink = new DataConsumer(this.controller);

            this.DoubleBuffered = true;
            this.Font = new Font(TerminalFont.FontFamily, 9.75f);

            this.controller.ShowCursor(true);
            this.controller.SendData += (sender, args) =>
            {
                OnInput(new InputEventArgs(Encoding.UTF8.GetString(args.Data)));
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

        internal string GetBuffer()
        {
            var buffer = new StringBuilder();

            var spans = this.controller.ViewPort.GetPageSpans(
                this.ViewTop,
                this.Rows,
                this.Columns,
                this.textSelection);

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
                this.textSelection);

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
                 $"ViewTop : {this.ViewTop}";

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

        public void PushText(string text)
        {
            lock (this.controller)
            {
                int oldTopRow = this.controller.ViewPort.TopRow;

                this.controllerSink.Push(Encoding.UTF8.GetBytes(text));

                if (this.controller.Changed)
                {
                    this.controller.ClearChanges();

                    if (oldTopRow != this.controller.ViewPort.TopRow && oldTopRow >= ViewTop)
                    {
                        ViewTop = this.controller.ViewPort.TopRow;
                    }

                    this.Invalidate();
                }
            }
        }

        //---------------------------------------------------------------------
        // Keybpard event handlers.
        //---------------------------------------------------------------------

        protected override bool ProcessDialogKey(Keys keyData)
        {
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

        internal bool SendKey(
            Keys keyCode,
            bool control,
            bool shift)
        {
            this.scrolling = false;

            if (IsKeySequence(
                NameFromKey(keyCode),
                control,
                shift))
            {
                //
                // This is a key sequence that needs to be
                // translated to some VT sequence.
                //
                return this.controller.KeyPressed(
                    NameFromKey(keyCode),
                    control,
                    shift);
            }
            else
            {
                //
                // This is a plain character. Typically, we'd
                // handle such input in KeyPress - but it is
                // difficult to ensure that KeyPress does not
                // handle any keys again that were already handled here.
                // Therefore, do the virtual key translation
                // manually here so that we do not need KeyPress
                // at all.
                //
                var ch = KeyUtil.CharFromKeyCode(keyCode);
                return  this.controller.KeyPressed(
                    ch,
                    control,
                    shift); ;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = SendKey(e.KeyCode, e.Control, e.Shift);
        }

        protected override void OnResize(EventArgs e)
        {
            Invalidate();
            base.OnResize(e);
        }

        //---------------------------------------------------------------------
        // Mouse event handlers.
        //---------------------------------------------------------------------

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
                // Scroll.

                int oldViewTop = this.ViewTop;

                this.ViewTop -= e.Delta / 40;
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

            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Paste clipboard.
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    this.controller.Paste(Encoding.UTF8.GetBytes(text));
                }
            }
            else if (e.Button == MouseButtons.Left)
            {
                // Begin a new selection. Memorize the location so that we
                // can start tracking.
                this.textSelection = null;
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
                    TextRange newSelection;
                    if (this.mouseDownPosition <= textPosition)
                    {
                        newSelection = new TextRange
                        {
                            Start = this.mouseDownPosition,
                            End = textPosition.OffsetBy(-1, 0)
                        };
                    }
                    else
                    {
                        newSelection = new TextRange
                        {
                            Start = textPosition,
                            End = this.mouseDownPosition
                        };
                    }

                    if (this.textSelection != newSelection)
                    {
                        textSelection = newSelection;

                        // Repaint to show selection.
                        Invalidate();
                    }
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.mouseDownPosition != null && this.textSelection != null)
            {
                // We're tracking a selection.

                var captured = this.controller.GetText(
                    this.textSelection.Start.Column,
                    this.textSelection.Start.Row,
                    this.textSelection.End.Column,
                    this.textSelection.End.Row);

                if (!string.IsNullOrEmpty(captured))
                {
                    // Copy to clipboard.
                    Clipboard.SetText(captured);
                }

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

        protected virtual void OnInput(InputEventArgs args)
        {
            this.InputReceived?.Invoke(this, args);
        }

        protected virtual void OnTerminalResize(TerminalResizeEventArgs args)
        {
            this.TerminalResized?.Invoke(this, args);
        }
    }

    public class InputEventArgs : EventArgs
    {
        public string Data { get; }

        public InputEventArgs(string data)
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
