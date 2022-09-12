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

using Google.Solutions.IapDesktop.Application.Data;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using System;

#pragma warning disable CA1058 // Types should not extend certain base types
#pragma warning disable CA2237 // Mark ISerializable types with serializable

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services
{
    public class ConnectionFailedException : ApplicationException, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        public ConnectionFailedException(
            string message,
            IHelpTopic helpTopic) : base(message)
        {
            this.Help = helpTopic;
        }

        public ConnectionFailedException(
            string message,
            IHelpTopic helpTopic,
            Exception innerException)
            : base(message, innerException)
        {
            this.Help = helpTopic;
        }
    }
}
