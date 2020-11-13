#
# Copyright 2019 Google LLC
#
# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.
#

#------------------------------------------------------------------------------
# Basic configuration
#------------------------------------------------------------------------------

PRODUCT_VERSION = 2.8
CONFIGURATION = Release
PLATFORM=x86

#------------------------------------------------------------------------------
# Ancillary variables
#------------------------------------------------------------------------------

!if ( "$(__BUILD_ENV_INITIALIZED)" == "" ) 
!	ERROR "build must be invoked by build.ps1"
!endif

!if ( "$(KOKORO_BUILD_NUMBER)" != "" ) 
BINARY_VERSION = $(PRODUCT_VERSION).$(KOKORO_BUILD_NUMBER)
!else
BINARY_VERSION = $(PRODUCT_VERSION).0
!endif

POWERSHELL = powershell -NoProfile -ExecutionPolicy Unrestricted

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

default: build

clean:
	@echo "========================================================"
	@echo "=== Cleaning up                                      ==="
	@echo "========================================================"

	$(POWERSHELL) $(MAKEDIR)\scripts\clean.ps1


check-copyright-headers:
	@echo "========================================================"
	@echo "=== Checking copyright headers                       ==="
	@echo "========================================================"

	$(POWERSHELL) $(MAKEDIR)\scripts\check-copyright-headers.ps1


Google.Solutions.IapDesktop\OAuthClient.cs:
	@echo "========================================================"
	@echo "=== Patching OAuth credentials                      ==="
	@echo "========================================================"

	copy "$(KOKORO_GFILE_DIR)\OAuthClient.cs" $@

nuget-packages:
	@echo "========================================================"
	@echo "=== Restoring nuget packages                         ==="
	@echo "========================================================"

	nuget restore

build: Google.Solutions.IapDesktop\OAuthClient.cs nuget-packages
	@echo "========================================================"
	@echo "=== Building solution                                ==="
	@echo "========================================================"

	msbuild /t:Rebuild /p:Configuration=$(CONFIGURATION);Platform=$(PLATFORM);AssemblyVersionNumber=$(BINARY_VERSION)

test: build
	@echo "========================================================"
	@echo "=== Testing solution                                 ==="
	@echo "========================================================"

	$(POWERSHELL) $(MAKEDIR)\scripts\run-tests.ps1
	
installer: build
	@echo "========================================================"
	@echo "=== Building installe                                ==="
	@echo "========================================================"

	??

symbols: build
	@echo "========================================================"
	@echo "=== Building installe                                ==="
	@echo "========================================================"

	??

#------------------------------------------------------------------------------
# Kokoro specific targets
#------------------------------------------------------------------------------

kokoro-iap-firewallrule:
	@echo "========================================================"
	@echo "=== Creating firewall rule                           ==="
	@echo "========================================================"

	gcloud auth activate-service-account --key-file=$(GOOGLE_APPLICATION_CREDENTIALS)
	gcloud compute firewall-rules create allow-ingress-from-iap \
		--direction=INGRESS \
		--action=allow \
		--rules=tcp \
		--source-ranges=35.235.240.0/20 \
		--project=$(GOOGLE_CLOUD_PROJECT)

kokoro-continuous-integration: kokoro-iap-firewallrule test installer symbols
