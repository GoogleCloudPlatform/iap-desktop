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
using System.Drawing;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Controls
{
	internal sealed class Caret : IDisposable
	{
		public bool Visible { get; private set; } = true;

		public IWin32Window Owner { get; }

		public Point Position
        {
			set
            {
				UnsafeNativeMethods.SetCaretPos(value.X, value.Y);
            }
			get
            {
				UnsafeNativeMethods.GetCaretPos(
					out UnsafeNativeMethods.POINT position);
				return new Point(position.X, position.Y);
			}
        }

		public Caret(IWin32Window owner, Size size)
		{
			this.Owner = owner;
			UnsafeNativeMethods.CreateCaret(owner.Handle, 0, size.Width, size.Height);
		}

		public void Hide()
		{
			if (this.Visible)
			{
				UnsafeNativeMethods.HideCaret(this.Owner.Handle);
				this.Visible = false;
			}
		}

		public void Show()
		{
			if (!this.Visible)
			{
				UnsafeNativeMethods.ShowCaret(this.Owner.Handle);
				this.Visible = true;
			}
		}

        public void Dispose()
        {
			UnsafeNativeMethods.DestroyCaret();
		}
    }
}
