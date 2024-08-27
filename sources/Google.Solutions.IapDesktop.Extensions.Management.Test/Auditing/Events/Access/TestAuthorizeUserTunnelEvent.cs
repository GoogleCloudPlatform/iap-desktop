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

using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Events.Access;
using Google.Solutions.IapDesktop.Extensions.Management.Auditing.Logs;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.Auditing.Events.Access
{
    [TestFixture]
    public class TestAuthorizeUserTunnelEvent : ApplicationFixtureBase
    {
        [Test]
        public void WhenSeverityIsInfo_ThenFieldsAreExtracted()
        {
            var json = @"
              {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'status': {},
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                   'callerIp': '3.4.5.6',
                   'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)',
                   'requestAttributes': {
                     'time': '2020-09-30T09:35:39.114684837Z',
                     'auth': {}
                   },
                   'destinationAttributes': {
                     'ip': '10.0.0.1',
                     'port': '3389'
                   }
                 },
                 'serviceName': 'iap.googleapis.com',
                 'methodName': 'AuthorizeUser',
                 'authorizationInfo': [
                   {
                     'resource': 'projects/111/iap_tunnel/zones/us-central1-a/instances/312951312222222222',
                     'permission': 'iap.tunnelInstances.accessViaIAP',
                     'granted': true,
                     'resourceAttributes': {
                       'service': 'iap.googleapis.com',
                       'type': 'iap.googleapis.com/TunnelInstance'
                     }
                   }
                 ],
                 'resourceName': '312951312222222222',
                 'request': {
                   'httpRequest': {
                     'url': ''
                   },
                   '@type': 'type.googleapis.com/cloud.security.gatekeeper.AuthorizeUserRequest'
                 },
                 'metadata': {
                   'request_id': '10362245139430470968',
                   'unsatisfied_access_levels': [
                     'accessPolicies/1072146573138/accessLevels/Windows_10_in_Germany',
                     'accessPolicies/1072146573138/accessLevels/mTLS_client_certificate'
                   ],
                   'device_state': 'Unknown',
                   'device_id': ''
                 }
               },
               'insertId': '822bjve2s5t9',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '312951312222222222',
                   'project_id': 'project-1',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-09-30T09:35:39.102788424Z',
               'severity': 'INFO',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
               'operation': {
                 'id': 'C6FU-2MNF-BVJL-2EGL-5MVS-IOMS',
                 'producer': 'iap.googleapis.com'
               },
               'receiveTimestamp': '2020-09-30T09:35:39.345791898Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(AuthorizeUserTunnelEvent.IsAuthorizeUserEvent(r));

            var e = (AuthorizeUserTunnelEvent)r.ToEvent();

            Assert.AreEqual(312951312222222222, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.Zone);
            Assert.AreEqual("project-1", e.ProjectId);
            Assert.AreEqual("INFO", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual("3.4.5.6", e.SourceHost);
            Assert.AreEqual("IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)", e.UserAgent);
            Assert.AreEqual("10.0.0.1", e.DestinationHost);
            Assert.AreEqual("3389", e.DestinationPort);

            CollectionAssert.IsEmpty(e.AccessLevels);
            Assert.IsNull(e.DeviceId);
            Assert.AreEqual("Unknown", e.DeviceState);

            Assert.AreEqual("Authorize tunnel from 3.4.5.6 to 10.0.0.1:3389 using IAP-Desktop/1.0.1.0", e.Message);
        }


        [Test]
        public void WhenMetadataContainsAccessLevel_ThenFieldsAreExtracted()
        {
            var json = @"
              {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'status': {},
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                   'callerIp': '3.4.5.6',
                   'callerSuppliedUserAgent': 'IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)',
                   'requestAttributes': {
                     'time': '2020-09-30T09:35:39.114684837Z',
                     'auth': {
                        'accessLevels': [
                        'accessPolicies/policy-1/accessLevels/level-1',
                        'accessPolicies/policy-2/accessLevels/level-2'
                        ]
                      }
                   },
                   'destinationAttributes': {
                     'ip': '10.0.0.1',
                     'port': '3389'
                   }
                 },
                 'serviceName': 'iap.googleapis.com',
                 'methodName': 'AuthorizeUser',
                 'authorizationInfo': [
                   {
                     'resource': 'projects/111/iap_tunnel/zones/us-central1-a/instances/312951312222222222',
                     'permission': 'iap.tunnelInstances.accessViaIAP',
                     'granted': true,
                     'resourceAttributes': {
                       'service': 'iap.googleapis.com',
                       'type': 'iap.googleapis.com/TunnelInstance'
                     }
                   }
                 ],
                 'resourceName': '312951312222222222',
                 'request': {
                   'httpRequest': {
                     'url': ''
                   },
                   '@type': 'type.googleapis.com/cloud.security.gatekeeper.AuthorizeUserRequest'
                 },
                 'metadata': {
                   'request_id': '10362245139430470968',
                   'unsatisfied_access_levels': [
                     'accessPolicies/1072146573138/accessLevels/Windows_10_in_Germany',
                     'accessPolicies/1072146573138/accessLevels/mTLS_client_certificate'
                   ],
                   'device_state': 'Normal',
                   'device_id': 'DEVICE-1'
                 }
               },
               'insertId': '822bjve2s5t9',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '312951312222222222',
                   'project_id': 'project-1',
                   'zone': 'us-central1-a'
                 }
               },
               'timestamp': '2020-09-30T09:35:39.102788424Z',
               'severity': 'INFO',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
               'operation': {
                 'id': 'C6FU-2MNF-BVJL-2EGL-5MVS-IOMS',
                 'producer': 'iap.googleapis.com'
               },
               'receiveTimestamp': '2020-09-30T09:35:39.345791898Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(AuthorizeUserTunnelEvent.IsAuthorizeUserEvent(r));

            var e = (AuthorizeUserTunnelEvent)r.ToEvent();

            Assert.AreEqual(312951312222222222, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.Zone);
            Assert.AreEqual("project-1", e.ProjectId);
            Assert.AreEqual("INFO", e.Severity);
            Assert.IsNull(e.Status);
            Assert.AreEqual("3.4.5.6", e.SourceHost);
            Assert.AreEqual("IAP-Desktop/1.0.1.0 (Microsoft ...),gzip(gfe)", e.UserAgent);
            Assert.AreEqual("10.0.0.1", e.DestinationHost);
            Assert.AreEqual("3389", e.DestinationPort);

            Assert.AreEqual(2, e.AccessLevels.Count());
            Assert.AreEqual(
                new AccessLevelLocator("policy-1", "level-1"),
                e.AccessLevels.First());
            Assert.AreEqual(
                new AccessLevelLocator("policy-2", "level-2"),
                e.AccessLevels.Last());

            Assert.AreEqual("DEVICE-1", e.DeviceId);
            Assert.AreEqual("Normal", e.DeviceState);

            Assert.AreEqual("Authorize tunnel from 3.4.5.6 to 10.0.0.1:3389 using IAP-Desktop/1.0.1.0", e.Message);
        }

        [Test]
        public void WhenSeverityIsError_ThenFieldsAreExtracted()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'status': {
                   'code': 7,
                   'message': 'Permission Denied.'
                 },
                 'authenticationInfo': {
                 },
                 'requestMetadata': {
                   'callerIp': '3.4.5.6',
                   'callerSuppliedUserAgent': 'gzip(gfe)',
                   'requestAttributes': {
                     'time': '2020-10-01T09:35:50.570492291Z',
                     'auth': {}
                   },
                   'destinationAttributes': {
                     'ip': '10.0.0.1',
                     'port': '3389'
                   }
                 },
                 'serviceName': 'iap.googleapis.com',
                 'methodName': 'AuthorizeUser',
                 'authorizationInfo': [
                   {
                     'resource': 'projects/111/iap_tunnel/zones/us-central1-a/instances/312951312222222222',
                     'permission': 'iap.tunnelInstances.accessViaIAP',
                     'resourceAttributes': {
                       'service': 'iap.googleapis.com',
                       'type': 'iap.googleapis.com/TunnelInstance'
                     }
                   }
                 ],
                 'resourceName': '312951312222222222',
                 'request': {
                   'httpRequest': {
                     'url': ''
                   },
                   '@type': 'type.googleapis.com/cloud.security.gatekeeper.AuthorizeUserRequest'
                 },
                 'metadata': {
                   'unsatisfied_access_levels': [],
                   'device_state': 'Unknown',
                   'device_id': ''
                 }
               },
               'insertId': 'p92rcge2oepz',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '312951312222222222',
                   'zone': 'us-central1-a',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-10-01T09:35:50.563179268Z',
               'severity': 'ERROR',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
               'operation': {
                 'id': '',
                 'producer': 'iap.googleapis.com'
               },
               'receiveTimestamp': '2020-10-01T09:35:51.222201769Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(AuthorizeUserTunnelEvent.IsAuthorizeUserEvent(r));

            var e = (AuthorizeUserTunnelEvent)r.ToEvent();

            Assert.AreEqual(312951312222222222, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.Zone);
            Assert.AreEqual("project-1", e.ProjectId);
            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(7, e.Status?.Code);
            Assert.AreEqual("3.4.5.6", e.SourceHost);
            Assert.AreEqual("gzip(gfe)", e.UserAgent);
            Assert.AreEqual("10.0.0.1", e.DestinationHost);
            Assert.AreEqual("3389", e.DestinationPort);

            CollectionAssert.IsEmpty(e.AccessLevels);
            Assert.IsNull(e.DeviceId);
            Assert.AreEqual("Unknown", e.DeviceState);

            Assert.AreEqual("Authorize tunnel from 3.4.5.6 to 10.0.0.1:3389 using gzip [Permission Denied.]", e.Message);
        }

        [Test]
        public void WhenFieldContentsAreMissing_ThenFieldsAreExtracted()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'status': {
                   'code': 7,
                   'message': 'Permission Denied.'
                 },
                 'authenticationInfo': {
                 },
                 'serviceName': 'iap.googleapis.com',
                 'methodName': 'AuthorizeUser',
                 'resourceName': '312951312222222222',
                 'request': {
                 },
                 'metadata': {
                 }
               },
               'insertId': 'p92rcge2oepz',
               'resource': {
                 'type': 'gce_instance',
                 'labels': {
                   'instance_id': '312951312222222222',
                   'zone': 'us-central1-a',
                   'project_id': 'project-1'
                 }
               },
               'timestamp': '2020-10-01T09:35:50.563179268Z',
               'severity': 'ERROR',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
               'operation': {
                 'id': '',
                 'producer': 'iap.googleapis.com'
               },
               'receiveTimestamp': '2020-10-01T09:35:51.222201769Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(AuthorizeUserTunnelEvent.IsAuthorizeUserEvent(r));

            var e = (AuthorizeUserTunnelEvent)r.ToEvent();

            Assert.AreEqual(312951312222222222, e.InstanceId);
            Assert.AreEqual("us-central1-a", e.Zone);
            Assert.AreEqual("project-1", e.ProjectId);
            Assert.AreEqual("ERROR", e.Severity);
            Assert.AreEqual(7, e.Status?.Code);
            Assert.IsNull(e.SourceHost);
            Assert.IsNull(e.UserAgent);
            Assert.IsNull(e.DestinationHost);
            Assert.IsNull(e.DestinationPort);

            CollectionAssert.IsEmpty(e.AccessLevels);
            Assert.IsNull(e.DeviceId);
            Assert.IsNull(e.DeviceState);

            Assert.AreEqual("Authorize tunnel from (unknown) to (unknown host):(unknown port) using (unknown agent) [Permission Denied.]", e.Message);
        }

        [Test]
        public void WhenRecordIsFromIapWeb_ThenIsAuthorizeUserEventReturnsFalse()
        {
            var json = @"
             {
               'protoPayload': {
                 '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                 'status': {
                   'code': 7,
                   'message': 'Permission Denied.'
                 },
                 'authenticationInfo': {},
                 'requestMetadata': {
                   'callerIp': '3.4.5.6',
                   'callerSuppliedUserAgent': 'Mozilla/5.0 (Windows NT 6.1; Win64; x64)',
                   'requestAttributes': {
                     'path': '/',
                     'host': 'foo.appspot.com',
                     'time': '2020-09-30T22:06:42.512958636Z',
                     'auth': {}
                   },
                   'destinationAttributes': {}
                 },
                 'serviceName': 'iap.googleapis.com',
                 'methodName': 'AuthorizeUser',
                 'authorizationInfo': [
                   {
                     'resource': 'projects/111/iap_web/compute/services/3154384839111111111/versions/bs_0',
                     'permission': 'iap.webServiceVersions.accessViaIAP',
                     'resourceAttributes': {
                       'service': 'iap.googleapis.com',
                       'type': 'iap.googleapis.com/WebServiceVersion'
                     }
                   }
                 ],
                 'resourceName': '3154384839111111111',
                 'request': {
                   'httpRequest': {
                     'url': 'https://foo.appspot.com/'
                   },
                   '@type': 'type.googleapis.com/cloud.security.gatekeeper.AuthorizeUserRequest'
                 },
                 'metadata': {
                   'device_state': 'Unknown',
                   'device_id': '',
                   'request_id': '111'
                 }
               },
               'insertId': '1ei33r8dzkm3',
               'resource': {
                 'type': 'gce_backend_service',
                 'labels': {
                   'project_id': 'project-1',
                   'backend_service_id': '',
                   'location': ''
                 }
               },
               'timestamp': '2020-09-30T22:06:42.508061205Z',
               'severity': 'ERROR',
               'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
               'receiveTimestamp': '2020-09-30T22:06:43.571840925Z'
             }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsFalse(AuthorizeUserTunnelEvent.IsAuthorizeUserEvent(r));
        }

        [Test]
        public void WhenAuthenticatedUsingWorkforceIdentitzFederation_ThenFieldsAreExtracted()
        {
            var json = @"
             {
              'protoPayload': {
                '@type': 'type.googleapis.com/google.cloud.audit.AuditLog',
                'status': {},
                'authenticationInfo': {
                  'principalSubject': 'principal://iam.googleapis.com/locations/global/workforcePools/pool-1/subject/subject@example.com'
                },
                'requestMetadata': {
                },
                'serviceName': 'iap.googleapis.com',
                'methodName': 'AuthorizeUser',
                'authorizationInfo': [
                  {
                    'resource': 'projects/111/iap_tunnel/zones/asia-southeast1-b/instances/2407816900433110951',
                    'permission': 'iap.tunnelInstances.accessViaIAP',
                    'granted': true,
                    'resourceAttributes': {
                      'service': 'iap.googleapis.com',
                      'type': 'iap.googleapis.com/TunnelInstance'
                    }
                  }
                ],
                'resourceName': '24078',
                'request': {
                  '@type': 'type.googleapis.com/cloud.security.gatekeeper.AuthorizeUserRequest',
                  'httpRequest': {
                    'url': ''
                  }
                },
                'metadata': {
                  'oauth_client_id': '',
                  'unsatisfied_access_levels': [
                    'accessPolicies/1/accessLevels/level-1'
                  ],
                  'device_state': 'Unknown',
                  'device_id': '',
                  'request_id': '964'
                }
              },
              'insertId': '1q5np',
              'resource': {
                'type': 'gce_instance',
                'labels': {
                  'instance_id': '2407816',
                  'zone': 'asia-southeast1-b',
                  'project_id': 'project-1'
                }
              },
              'timestamp': '2023-08-25T00:28:09.909737076Z',
              'severity': 'INFO',
              'logName': 'projects/project-1/logs/cloudaudit.googleapis.com%2Fdata_access',
              'operation': {
                'id': 'ZJHS-F5Y7-C6CN-PK3O-PKRP-DHYX',
                'producer': 'iap.googleapis.com'
              },
              'receiveTimestamp': '2023-08-25T00:28:10.903692392Z'
            }";

            var r = LogRecord.Deserialize(json)!;
            Assert.IsTrue(AuthorizeUserTunnelEvent.IsAuthorizeUserEvent(r));

            var e = (AuthorizeUserTunnelEvent)r.ToEvent();

            Assert.AreEqual(
                "principal://iam.googleapis.com/locations/global/workforcePools/pool-1/subject/subject@example.com",
                e.Principal);
            Assert.AreEqual(2407816, e.InstanceId);
            Assert.AreEqual("asia-southeast1-b", e.Zone);
            Assert.AreEqual("project-1", e.ProjectId);
            Assert.AreEqual("INFO", e.Severity);
            Assert.IsNull(e.SourceHost);
            Assert.IsNull(e.UserAgent);
            Assert.IsNull(e.DestinationHost);
            Assert.IsNull(e.DestinationPort);

            CollectionAssert.IsEmpty(e.AccessLevels);
            Assert.IsNull(e.DeviceId);
            Assert.AreEqual("Unknown", e.DeviceState);

            Assert.AreEqual("Authorize tunnel from (unknown) to (unknown host):(unknown port) using (unknown agent)", e.Message);
        }
    }
}
