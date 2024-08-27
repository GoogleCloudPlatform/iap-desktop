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
using System;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Events
{
    [TestFixture]
    public class TestEventFactory : ApplicationFixtureBase
    {
        [Test]
        public void WhenRecordIsLegacyRecord_ThenFromRecordThrowsArgumentException()
        {
            var json = @"
                {
                   'insertId': '6e43rqfayr3qk',
                   'jsonPayload': {
                     'event_subtype': 'compute.instances.preempted',
                     'trace_id': 'systemevent-1589212587537-5a56163c0b559-3e94b149-65935fdf',
                     'info': [
                       {
                         'code': 'STATUS_MESSAGE',
                         'detail_message': 'Instance was preempted.'
                       }
                     ],
                     'version': '1.2',
                     'actor': {
                       'user': 'system'
                     },
                     'operation': {
                       'zone': 'us-central1-a',
                       'name': 'systemevent-1589212587537-5a56163c0b559-3e94b149-65935fdf',
                       'type': 'operation',
                       'id': '112233'
                     },
                     'resource': {
                       'zone': 'us-central1-a',
                       'name': 'instance-1',
                       'type': 'instance',
                       'id': '112233'
                     },
                     'event_timestamp_us': '1589212604945365',
                     'event_type': 'GCE_OPERATION_DONE'
                   },
                   'resource': {
                     'type': 'gce_instance',
                     'labels': {
                       'instance_id': '112233',
                       'project_id': 'project-1',
                       'zone': 'us-central1-a'
                     }
                   },
                   'timestamp': '2020-05-11T15:56:44.945365Z',
                   'severity': 'INFO',
                   'labels': {
                     'compute.googleapis.com/resource_zone': 'us-central1-a',
                     'compute.googleapis.com/resource_name': 'instance-1',
                     'compute.googleapis.com/resource_id': '112233',
                     'compute.googleapis.com/resource_type': 'instance'
                   },
                   'logName': 'projects/project-1/logs/compute.googleapis.com%2Factivity_log',
                   'receiveTimestamp': '2020-05-11T15:56:45.021692902Z'
                 }";

            var r = LogRecord.Deserialize(json)!;
            Assert.Throws<ArgumentException>(() => EventFactory.FromRecord(r));
        }
    }
}
