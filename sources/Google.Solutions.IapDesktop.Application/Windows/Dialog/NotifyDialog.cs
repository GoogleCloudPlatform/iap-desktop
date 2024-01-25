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
using Google.Solutions.IapDesktop.Application.Properties;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    public interface INotifyDialog
    {
        /// <summary>
        /// Show baloon in task bar.
        /// </summary>
        void ShowBalloon(
            string title,
            string message);
    }

    public class NotifyDialog : INotifyDialog
    {
        public void ShowBalloon(string title, string message)
        { 
            title.ExpectNotEmpty(nameof(title));
            message.ExpectNotEmpty(nameof(message));

            var baloon = new NotifyIcon()
            {
                Visible = true,
                Icon = Resources.logo,
                BalloonTipIcon = ToolTipIcon.None,
                BalloonTipTitle = title,
                BalloonTipText = message
            };

            //
            // NB. If we call Dispose before the timeout
            // elapses, the baloon shows an ugly header. 
            //
            baloon.BalloonTipClosed += (_, __) => baloon.Dispose();
            baloon.ShowBalloonTip(10 * 1000);
        }
    }
}
