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

using Google.Solutions.Apis.Diagnostics;
using System;
using System.Diagnostics;

namespace Google.Solutions.Apis
{
    public abstract class ClientException : Exception
    {
        protected ClientException(string message) : base(message)
        {
        }

        protected ClientException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ResourceNotFoundException : ClientException
    {
        public ResourceNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class ResourceAccessDeniedException : ClientException, IExceptionWithHelpTopic
    {
        public IHelpTopic? Help { get; }

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

    public class ResourceAccessDeniedByVpcScPolicyException : ResourceAccessDeniedException
    {
        public ResourceAccessDeniedByVpcScPolicyException(GoogleApiException e)
            : base("Your organization's VPC service control policy doesn't permit " +
                    "access to this resource.\n\n" +
                    $"Unique ID: {e.VpcServiceControlTroubleshootingId()}",
                e.VpcServiceControlTroubleshootingLink() ?? HelpTopics.ProjectAccessControl,
                e)
        {
            Debug.Assert(e.IsAccessDeniedByVpcServiceControlPolicy());
        }
    }
}
