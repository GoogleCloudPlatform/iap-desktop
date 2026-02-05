//
// Copyright 2019 Google LLC
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

using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Events
{
    [TestFixture]
    public class TestUnknownEvent : ApplicationFixtureBase
    {
        [Test]
        public void ToEvent_WhenSeverityIsNotice()
        {
            var json = @"
            {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                 },
                 'serviceName': 'compute.googleapis.com',
                 'methodName': 'v1.compute.instances.unknown',
                 'authorizationInfo': [
                 ],
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                 },
                 'response': {
                 },
                 'resourceLocation': {
                   'currentLocations': [
                     'us-central1-a'
                   ]
                 }
               },
               'insertId': 'vcq6epd7n72',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'project_id': 'project-1',
                   'instance_id': '4894051111144103',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-05-04T13:56:26.405Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1588600586345-5a4d2e5a39c56-47d0ce05-a9d7073c',
                 'producer': 'compute.googleapis.com',
                 'first': true
               },
               'receiveTimestamp': '2020-05-04T13:56:27.582777461Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsInstanceOf<UnknownEvent>(r.ToEvent());
            var e = (UnknownEvent)r.ToEvent();

            Assert.That(e.Severity, Is.EqualTo("NOTICE"));
        }

        [Test]
        public void ToEvent_WhenSeverityIsError()
        {
            Assert.Inconclusive();
        }
    }
}