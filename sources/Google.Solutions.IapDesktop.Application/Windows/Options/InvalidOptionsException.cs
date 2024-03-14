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

using Google.Solutions.Apis.Diagnostics;
using Google.Solutions.Common.Util;
using System;

namespace Google.Solutions.IapDesktop.Application.Windows.Options
{
    internal class InvalidOptionsException : ArgumentException, IExceptionWithHelpTopic
    {
        public InvalidOptionsException(
            string message,
            IHelpTopic helpTopic) : base(message)
        {
            this.Help = helpTopic.ExpectNotNull(nameof(helpTopic));
        }

        public InvalidOptionsException(
            string message,
            Exception innerException,
            IHelpTopic helpTopic)
            : base(message, innerException)
        {
            this.Help = helpTopic.ExpectNotNull(nameof(helpTopic));
        }

        public IHelpTopic Help { get; }
    }
}
