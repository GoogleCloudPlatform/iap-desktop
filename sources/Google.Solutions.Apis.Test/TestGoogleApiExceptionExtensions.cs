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

using NUnit.Framework;

namespace Google.Solutions.Apis.Test
{
    [TestFixture]
    public class TestGoogleApiExceptionExtensions
    {
        //---------------------------------------------------------------------
        // VpcServiceControlTroubleshootingId.
        //---------------------------------------------------------------------

        [Test]
        public void VpcServiceControlTroubleshootingId_WhenVpcScError()
        {
            var e = new GoogleApiException("test")
            {
                Error = new Google.Apis.Requests.RequestError()
                {
                    ErrorResponseContent = @"{
                      'error': {
                        'code': 403,
                        'message': 'Request is prohibited by organizations policy. vpcServiceControlsUniqueIdentifier: ID123',
                        'errors': [
                          {
                            'message': 'Request is prohibited by organizations policy. vpcServiceControlsUniqueIdentifier: ID123',
                            'domain': 'global',
                            'reason': 'forbidden'
                          }
                        ],
                        'status': 'PERMISSION_DENIED',
                        'details': [
                          {
                            '@type': 'type.googleapis.com/google.rpc.PreconditionFailure',
                            'violations': [
                              {
                                'type': 'VPC_SERVICE_CONTROLS',
                                'description': 'ID123'
                              }
                            ]
                          },
                          {
                            '@type': 'type.googleapis.com/google.rpc.ErrorInfo',
                            'reason': 'SECURITY_POLICY_VIOLATED',
                            'domain': 'googleapis.com',
                            'metadatas': {
                              'uid': 'ID123',
                              'service': 'compute.googleapis.com',
                              'consumer': 'projects/project-1'
                            }
                          }
                        ]
                      }
                    }"
                }
            };

            Assert.That(e.VpcServiceControlTroubleshootingId(), Is.EqualTo("ID123"));
        }

        [Test]
        public void VpcServiceControlTroubleshootingId_WhenAccessDeniedError()
        {
            var e = new GoogleApiException("test")
            {
                Error = new Google.Apis.Requests.RequestError()
                {
                    ErrorResponseContent = @"{
                      'error': {
                        'code': 403,
                        'message': 'Denied for some reason',
                        'errors': [
                          {
                            'message': 'Denied for some reason',
                            'domain': 'global',
                            'reason': 'forbidden'
                          }
                        ],
                        'status': 'PERMISSION_DENIED',
                        'details': []
                      }
                    }"
                }
            };

            Assert.That(e.VpcServiceControlTroubleshootingId(), Is.Null);
        }
    }
}
