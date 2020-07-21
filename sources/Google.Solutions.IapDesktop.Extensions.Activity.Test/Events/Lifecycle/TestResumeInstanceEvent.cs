﻿//
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

using Google.Solutions.Common.Locator;
using Google.Solutions.IapDesktop.Extensions.Activity.Events;
using Google.Solutions.IapDesktop.Extensions.Activity.Events.Lifecycle;
using Google.Solutions.IapDesktop.Extensions.Activity.Logs;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Test.Events.Lifecycle
{
    [TestFixture]
    public class TestResumeInstanceEvent : FixtureBase
    {
        [Test]
        public void WhenSeverityIsNotice_ThenFieldsAreExtracted()
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
                 'methodName': 'v1.compute.instances.resume',
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.resume'
                 }
               },
               'insertId': 'rr3nw0dcqz0',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'project_id': 'project-1',
                   'instance_id': '1111111245427925863'
                 }
               },
               'timestamp': '2020-06-18T07:57:03.406Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1592467006073-5a8571e36aa91-ec1b6663-31feb7e3',
                 'producer': 'compute.googleapis.com',
                 'last': true
               },
               'receiveTimestamp': '2020-06-18T07:57:04.444187095Z'
             }}";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(ResumeInstanceEvent.IsResumeInstanceEvent(r));

            var e = (ResumeInstanceEvent)r.ToEvent();

            Assert.AreEqual(1111111245427925863, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }

        [Test]
        public void WhenSeverityIsNoticeAndVersionIsBeta_ThenFieldsAreExtracted()
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
                 'methodName': 'beta.compute.instances.resume',
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.resume'
                 }
               },
               'insertId': 'rr3nw0dcqz0',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'project_id': 'project-1',
                   'instance_id': '1111111245427925863'
                 }
               },
               'timestamp': '2020-06-18T07:57:03.406Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1592467006073-5a8571e36aa91-ec1b6663-31feb7e3',
                 'producer': 'compute.googleapis.com',
                 'last': true
               },
               'receiveTimestamp': '2020-06-18T07:57:04.444187095Z'
             }}";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(ResumeInstanceEvent.IsResumeInstanceEvent(r));

            var e = (ResumeInstanceEvent)r.ToEvent();

            Assert.AreEqual(1111111245427925863, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }

        [Test]
        public void WhenSeverityIsNoticeAndVersionIsAlpha_ThenFieldsAreExtracted()
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
                 'methodName': 'alpha.compute.instances.resume',
                 'resourceName': 'projects/project-1/zones/us-central1-a/instances/instance-1',
                 'request': {
                   '@type': 'type.googleapis.com/compute.instances.resume'
                 }
               },
               'insertId': 'rr3nw0dcqz0',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'zone': 'us-central1-a',
                   'project_id': 'project-1',
                   'instance_id': '1111111245427925863'
                 }
               },
               'timestamp': '2020-06-18T07:57:03.406Z',
               'severity': 'NOTICE',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Factivity',
               'operation': {
                 'id': 'operation-1592467006073-5a8571e36aa91-ec1b6663-31feb7e3',
                 'producer': 'compute.googleapis.com',
                 'last': true
               },
               'receiveTimestamp': '2020-06-18T07:57:04.444187095Z'
             }}";

            var r = LogRecord.Deserialize(json);
            Assert.IsTrue(ResumeInstanceEvent.IsResumeInstanceEvent(r));

            var e = (ResumeInstanceEvent)r.ToEvent();

            Assert.AreEqual(1111111245427925863, e.InstanceId);
            Assert.AreEqual("instance-1", e.InstanceReference.Name);
            Assert.AreEqual("us-central1-a", e.InstanceReference.Zone);
            Assert.AreEqual("project-1", e.InstanceReference.ProjectId);
            Assert.AreEqual("NOTICE", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual(
                new InstanceLocator("project-1", "us-central1-a", "instance-1"),
                e.InstanceReference);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            Assert.Inconclusive();
        }
    }
}