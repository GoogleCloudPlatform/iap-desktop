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

PRODUCT_VERSION = 2.9
CONFIGURATION = Release
PLATFORM=x86

!if ( "$(KOKORO_BUILD_NUMBER)" != "" ) 
BINARY_VERSION = $(PRODUCT_VERSION).$(KOKORO_BUILD_NUMBER)
!else
BINARY_VERSION = $(PRODUCT_VERSION).0
!endif

MAIN_ASSEMBLY = Google.Solutions.IapDesktop\bin\$(CONFIGURATION)\IapDesktop.exe

INSTALLER_BUILD_PACKAGE = installer\bin\$(CONFIGURATION)\Installer-$(BINARY_VERSION).zip
INSTALLER_PACKAGE = installer\bin\$(CONFIGURATION)\IapDesktop-$(BINARY_VERSION).msi
SYMBOLS_PACKAGE = installer\bin\$(CONFIGURATION)\Symbols-$(BINARY_VERSION).zip

NUNIT_ASSEMBLIES = \
	Google.Solutions.Common.Test\bin\$(CONFIGURATION)\Google.Solutions.Common.Test.dll	\
	Google.Solutions.IapTunneling.Test\bin\release\Google.Solutions.IapTunneling.Test.dll \
	Google.Solutions.IapDesktop.Extensions.Activity.Test\bin\release\Google.Solutions.IapDesktop.Extensions.Activity.Test.dll \
	Google.Solutions.IapDesktop.Extensions.Os.Test\bin\release\Google.Solutions.IapDesktop.Extensions.Os.Test.dll \
	Google.Solutions.IapDesktop.Extensions.Rdp.Test\bin\release\Google.Solutions.IapDesktop.Extensions.Rdp.Test.dll \
	Google.Solutions.IapDesktop.Application.Test\bin\release\Google.Solutions.IapDesktop.Application.Test.dll

NUNIT_COVERAGE_FILTER = \
	+[Google.Solutions.Common]* \
	+[Google.Solutions.IapTunneling]* \
	+[Google.Solutions.IapDesktop.Extensions.Activity]* \
	+[Google.Solutions.IapDesktop.Extensions.Os]* \
	+[Google.Solutions.IapDesktop.Extensions.Rdp]* \
	+[Google.Solutions.IapDesktop.Application]*

#------------------------------------------------------------------------------
# Ancillary variables
#------------------------------------------------------------------------------

!if ( "$(__BUILD_ENV_INITIALIZED)" == "" ) 
!	ERROR "build must be invoked by build.ps1"
!endif

POWERSHELL = powershell -NoProfile -ExecutionPolicy Unrestricted

#------------------------------------------------------------------------------
# Output targets
#------------------------------------------------------------------------------

default: kokoro-continuous-integration

Google.Solutions.IapDesktop\OAuthClient.cs:
	@echo "========================================================"
	@echo "=== Patching OAuth credentials                      ==="
	@echo "========================================================"

	copy "$(KOKORO_GFILE_DIR)\OAuthClient.cs" $@

$(MAIN_ASSEMBLY) $(NUNIT_ASSEMBLIES): Google.Solutions.IapDesktop\OAuthClient.cs
	@echo "========================================================"
	@echo "=== Building solution                                ==="
	@echo "========================================================"

	msbuild /t:Rebuild /p:Configuration=$(CONFIGURATION);Platform=$(PLATFORM);AssemblyVersionNumber=$(BINARY_VERSION)
	
$(MAKEDIR)\installer\obj\$(CONFIGURATION): $(MAIN_ASSEMBLY)
	@echo "========================================================"
	@echo "=== Creating staging area for installer              ==="
	@echo "========================================================"
	
	md $(MAKEDIR)\installer\obj\$(CONFIGURATION)
		
	copy $(MAKEDIR)\installer\*.* \
		$(MAKEDIR)\installer\obj\$(CONFIGURATION)
	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(CONFIGURATION)\*.exe* \
		$(MAKEDIR)\installer\obj\$(CONFIGURATION)
	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(CONFIGURATION)\*.dll \
		$(MAKEDIR)\installer\obj\$(CONFIGURATION)

$(INSTALLER_PACKAGE).wixobj: \
			$(MAKEDIR)\installer\Product.wxs \
			$(MAKEDIR)\installer\obj\$(CONFIGURATION)
	@echo "========================================================"
	@echo "=== Compiling installer                              ==="
	@echo "========================================================"

	candle \
		-nologo \
		-out $(INSTALLER_PACKAGE).wixobj \
		-dCONFIGURATION=$(CONFIGURATION) \
		-dVERSION=$(BINARY_VERSION) \
		-dBASEDIR=$(MAKEDIR)\installer \
		-arch x86 \
		-ext packages\Wix.$(Wix)\tools\WixUIExtension.dll \
		-ext packages\Wix.$(Wix)\tools\WixUtilExtension.dll \
		 $(MAKEDIR)\installer\Product.wxs

$(INSTALLER_PACKAGE): $(INSTALLER_PACKAGE).wixobj
	@echo "========================================================"
	@echo "=== Linking installer                                ==="
	@echo "========================================================"

	light \
		-nologo \
		-out $@ \
		-sw1076 \
		-cultures:null \
		-ext packages\Wix.$(Wix)\tools\WixUIExtension.dll \
		-ext packages\Wix.$(Wix)\tools\WixUtilExtension.dll \
		$(INSTALLER_PACKAGE).wixobj

$(INSTALLER_BUILD_PACKAGE): $(MAKEDIR)\installer\obj\$(CONFIGURATION)
	@echo "========================================================"
	@echo "=== Packaging installer staging area                 ==="
	@echo "========================================================"
	
	$(POWERSHELL) $(MAKEDIR)\scripts\create-zip.ps1 \
		-ZipFile $(INSTALLER_BUILD_PACKAGE) \
		-Path makefile, $(MAKEDIR)\installer
		
$(SYMBOLS_PACKAGE): $(MAIN_ASSEMBLY)
	@echo "========================================================"
	@echo "=== Packaging symbols                                ==="
	@echo "========================================================"

	if not exist $(MAKEDIR)\installer\bin\$(CONFIGURATION)\symbols \
		md $(MAKEDIR)\installer\bin\$(CONFIGURATION)\symbols

	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(CONFIGURATION)\*.pdb \
		$(MAKEDIR)\installer\bin\$(CONFIGURATION)\symbols

	$(POWERSHELL) $(MAKEDIR)\scripts\create-zip.ps1 $(SYMBOLS_PACKAGE) \
		$(MAKEDIR)\installer\bin\$(CONFIGURATION)\symbols\*

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

cmd:
	@echo "========================================================"
	@echo "=== Opening command prompt                           ==="
	@echo "========================================================"

	cmd

clean:
	@echo "========================================================"
	@echo "=== Cleaning up                                      ==="
	@echo "========================================================"

	$(POWERSHELL) $(MAKEDIR)\scripts\clean.ps1

restore:
	@echo "========================================================"
	@echo "=== Restoring nuget packages                         ==="
	@echo "========================================================"

	nuget restore

check-copyright-headers:
	@echo "========================================================"
	@echo "=== Checking copyright headers                       ==="
	@echo "========================================================"

	$(POWERSHELL) $(MAKEDIR)\scripts\check-copyright-headers.ps1

build: $(MAIN_ASSEMBLY)

test: $(NUNIT_ASSEMBLIES)
	@echo "========================================================"
	@echo "=== Testing solution                                 ==="
	@echo "========================================================"

	OpenCover.Console.exe \
		-register:user \
		-returntargetcode \
		-target:nunit3-console.exe \
		"-targetargs:$(NUNIT_ASSEMBLIES) --result=sponge_log.xml;transform=..\kokoro\nunit-to-sponge.xsl" \
		-filter:"$(NUNIT_COVERAGE_FILTER)" \
		"-excludebyattribute:*.SkipCodeCoverage*;*CompilerGenerated*" \
		-output:opencovertests.xml

	ReportGenerator.exe \
		-reports:opencovertests.xml \
		-targetdir:coveragereport \
		-reporttypes:HTML
	
installer: $(INSTALLER_PACKAGE) $(INSTALLER_BUILD_PACKAGE) $(SYMBOLS_PACKAGE)

#------------------------------------------------------------------------------
# Kokoro specific targets
#------------------------------------------------------------------------------

kokoro-iap-firewallrule:
	@echo "========================================================"
	@echo "=== Creating firewall rule                           ==="
	@echo "========================================================"

	gcloud auth activate-service-account --key-file=$(GOOGLE_APPLICATION_CREDENTIALS)
	-gcloud compute firewall-rules create allow-ingress-from-iap \
		--direction=INGRESS \
		--action=allow \
		--rules=tcp \
		--source-ranges=35.235.240.0/20 \
		--project=$(GOOGLE_CLOUD_PROJECT)

kokoro-continuous-integration: check-copyright-headers kokoro-iap-firewallrule installer test
