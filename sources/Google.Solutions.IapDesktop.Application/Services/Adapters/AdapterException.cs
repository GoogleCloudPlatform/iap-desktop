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
using Google.Solutions.IapDesktop.Core.Diagnostics;
using System;
using System.Runtime.Serialization;

namespace Google.Solutions.IapDesktop.Application.Services.Adapters
{

    [Serializable]
    public class AdapterException : Exception
    {
        protected AdapterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AdapterException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public class ResourceNotFoundException : AdapterException
    {
        protected ResourceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ResourceNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public class ResourceAccessDeniedException : AdapterException, IExceptionWithHelpTopic
    {
        public IHelpTopic Help { get; }

        protected ResourceAccessDeniedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ResourceAccessDeniedException(
            string message,
            Exception inner)
            : base(message, inner)
        {
        }

        public ResourceAccessDeniedException(
            string message,
            IHelpTopic helpTopic,
            Exception inner)
            : base(message, inner)
        {
            this.Help = helpTopic;
        }
    }
}
