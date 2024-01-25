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

PRODUCT_VERSION = 2.40
CONFIGURATION = Release
PLATFORM = x86
TARGET_FRAMEWORK = net47

!if ( "$(KOKORO_BUILD_NUMBER)" != "" ) 
BUILD_NUMBER = $(KOKORO_BUILD_NUMBER)
!else
BUILD_NUMBER = 1
!endif

BINARY_VERSION = $(PRODUCT_VERSION).$(BUILD_NUMBER)

MAIN_ASSEMBLY = Google.Solutions.IapDesktop\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\IapDesktop.exe

INSTALLER_SNAPSHOT = installer\bin\$(CONFIGURATION)\InstallerSnapshot-$(BINARY_VERSION)-$(PLATFORM)-$(CONFIGURATION).zip
INSTALLER_PACKAGE  = installer\bin\$(CONFIGURATION)\IapDesktop-$(BINARY_VERSION)-$(PLATFORM)-$(CONFIGURATION).msi
SYMBOLS_PACKAGE    = installer\bin\$(CONFIGURATION)\Symbols-$(BINARY_VERSION)-$(PLATFORM)-$(CONFIGURATION).zip
POLICIES_PACKAGE   = installer\bin\$(CONFIGURATION)\PolicyTemplates-$(BINARY_VERSION)-$(PLATFORM)-$(CONFIGURATION).zip

NUNIT_ASSEMBLIES = \
	Google.Solutions.Apis.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.Apis.Test.dll	\
	Google.Solutions.Platform.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.Platform.Test.dll	\
	Google.Solutions.Common.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.Common.Test.dll	\
	Google.Solutions.Iap.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.Iap.Test.dll \
	Google.Solutions.Ssh.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.Ssh.Test.dll \
	Google.Solutions.Mvvm.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.Mvvm.Test.dll \
	Google.Solutions.IapDesktop.Core.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.IapDesktop.Core.Test.dll \
	Google.Solutions.IapDesktop.Application.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.IapDesktop.Application.Test.dll \
	Google.Solutions.IapDesktop.Extensions.Management.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.IapDesktop.Extensions.Management.Test.dll \
	Google.Solutions.IapDesktop.Extensions.Session.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.IapDesktop.Extensions.Session.Test.dll \
	Google.Solutions.IapDesktop.Extensions.Explorer.Test\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\Google.Solutions.IapDesktop.Extensions.Explorer.Test.dll

NUNIT_COVERAGE_FILTER = \
	+[Google.Solutions.Apis]* \
	+[Google.Solutions.Platform]* \
	+[Google.Solutions.Common]* \
	+[Google.Solutions.Iap]* \
	+[Google.Solutions.Ssh]* \
	+[Google.Solutions.Mvvm]* \
	+[Google.Solutions.IapDesktop.Core]* \
	+[Google.Solutions.IapDesktop.Application]* \
	+[Google.Solutions.IapDesktop.Extensions.Management]* \
	+[Google.Solutions.IapDesktop.Extensions.Session]* \
	+[Google.Solutions.IapDesktop.Extensions.Explorer]*
	
SIGNTOOL_FLAGS = /v /tr http://timestamp.digicert.com /n Google /a /fd sha256 /td sha256
NUGET_PACKAGEES = $(USERPROFILE)\.nuget\packages

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

themes: Google.Solutions.IapDesktop.Application\Theme\Resources\Dark.vstheme \
        Google.Solutions.IapDesktop.Application\Theme\Resources\Light.vstheme
    !$(POWERSHELL) $(MAKEDIR)\scripts\gzip.ps1 -File $**

Google.Solutions.IapDesktop\OAuthClient.cs:
	@echo "========================================================"
	@echo "=== Patching OAuth credentials                      ==="
	@echo "========================================================"

	copy "$(KOKORO_GFILE_DIR)\OAuthClient.cs" $@

$(MAIN_ASSEMBLY) $(NUNIT_ASSEMBLIES): themes Google.Solutions.IapDesktop\OAuthClient.cs
	@echo "========================================================"
	@echo "=== Building solution                                ==="
	@echo "========================================================"
    
	msbuild /t:Rebuild /p:Configuration=$(CONFIGURATION);Platform=$(PLATFORM);Version=$(BINARY_VERSION)
	
!if ( "$(SKIP_ASSEMBLY_BUILD)" == "" ) 
$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION): $(MAIN_ASSEMBLY)
	@echo "========================================================"
	@echo "=== Creating staging area for installer              ==="
	@echo "========================================================"
    
    if not exist $(MAKEDIR)\installer\obj \
        md $(MAKEDIR)\installer\obj
    if not exist $(MAKEDIR)\installer\obj\$(PLATFORM) \
        md $(MAKEDIR)\installer\obj\$(PLATFORM)
    if not exist $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION) \
        md $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
		
	copy $(MAKEDIR)\installer\*.* \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\*.exe* \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\*.dll \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\*.ps1 \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\*.cmd \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
!endif

$(INSTALLER_PACKAGE).wixobj: \
			$(MAKEDIR)\installer\Product.wxs \
			$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
	@echo "========================================================"
	@echo "=== Compiling installer                              ==="
	@echo "========================================================"

	candle \
		-nologo \
		-out $(INSTALLER_PACKAGE).wixobj \
		-dCONFIGURATION=$(CONFIGURATION) \
		-dPLATFORM=$(PLATFORM) \
		-dVERSION=$(BINARY_VERSION) \
		-dBASEDIR=$(MAKEDIR)\installer \
		-arch x86 \
		-ext WixUIExtension.dll \
		-ext WixUtilExtension.dll \
		 $(MAKEDIR)\installer\Product.wxs

$(INSTALLER_PACKAGE): $(INSTALLER_PACKAGE).wixobj
	@echo "========================================================"
	@echo "=== Linking installer                                ==="
	@echo "========================================================"

!if ( "$(CODESIGN)" != "" ) 
	signtool sign $(SIGNTOOL_FLAGS) $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\Google.*.dll
	signtool sign $(SIGNTOOL_FLAGS) $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\libssh2.dll
	signtool sign $(SIGNTOOL_FLAGS) $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\vtnetcore.dll
	signtool sign $(SIGNTOOL_FLAGS) $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\WeifenLuo.WinFormsUI.Docking*.dll
	signtool sign $(SIGNTOOL_FLAGS) $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\*Interop*.dll
	signtool sign $(SIGNTOOL_FLAGS) $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\IapDesktop.exe
	
	signtool verify /pa $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\*.exe
	signtool verify /pa $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\*.dll
!endif

	light \
		-nologo \
		-out $@ \
		-sw1076 \
		-cultures:null \
		-loc $(MAKEDIR)\installer\Product.wxl \
		-ext WixUIExtension.dll \
		-ext WixUtilExtension.dll \
		$(INSTALLER_PACKAGE).wixobj

!if ( "$(CODESIGN)" != "" ) 
	signtool sign $(SIGNTOOL_FLAGS) $(INSTALLER_PACKAGE)
!endif

$(INSTALLER_SNAPSHOT): $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)
	@echo "========================================================"
	@echo "=== Packaging installer snapshot                     ==="
	@echo "========================================================"
	
    -1 robocopy $(MAKEDIR)\installer\bin $(MAKEDIR)\installer\bin.original /E /MOVE
    
	$(POWERSHELL) $(MAKEDIR)\scripts\create-zip.ps1 \
		-ZipFile $(INSTALLER_SNAPSHOT) \
		-Path $(MAKEDIR)\makefile, \
			  $(MAKEDIR)\build.ps1, \
			  $(MAKEDIR)\installer, \
			  $(NUGET_PACKAGEES)\Wix\$(WIX)\tools

    - robocopy $(MAKEDIR)\installer\bin.original $(MAKEDIR)\installer\bin /E /MOVE
    
$(SYMBOLS_PACKAGE): $(MAIN_ASSEMBLY)
	@echo "========================================================"
	@echo "=== Packaging symbols                                ==="
	@echo "========================================================"

	if not exist $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\symbols \
		md $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\\symbols

	copy $(MAKEDIR)\Google.Solutions.IapDesktop\bin\$(PLATFORM)\$(CONFIGURATION)\$(TARGET_FRAMEWORK)\*.pdb \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\symbols

	$(POWERSHELL) $(MAKEDIR)\scripts\create-zip.ps1 $(SYMBOLS_PACKAGE) \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\symbols\*
        
$(POLICIES_PACKAGE):
	@echo "========================================================"
	@echo "=== Packaging policy templates                       ==="
	@echo "========================================================"

	if not exist $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies \
		md $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies
	if not exist $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies\en-us \
		md $(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies\en-us

	copy $(MAKEDIR)\installer\policies\*.admx \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies
	copy $(MAKEDIR)\installer\policies\*.adml \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies\en-us

	$(POWERSHELL) $(MAKEDIR)\scripts\create-zip.ps1 $(POLICIES_PACKAGE) \
		$(MAKEDIR)\installer\obj\$(PLATFORM)\$(CONFIGURATION)\policies\*

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

# NB. The OpenCover version must match the version of the CLR installed
test: $(NUNIT_ASSEMBLIES)
	@echo "========================================================"
	@echo "=== Testing solution                                 ==="
	@echo "========================================================"

	OpenCover.Console.exe \
		-register:user \
		-returntargetcode \
		-target:nunit3-console.exe \
		"-targetargs:$(NUNIT_ASSEMBLIES) --result=sponge_log.xml;transform=kokoro\nunit-to-sponge.xsl --agents=1 --where ""cat!=InteractiveTest and cat!=RdpTest and cat!=ProxyAutoconfigTest""" \
		-filter:"$(NUNIT_COVERAGE_FILTER)" \
		"-excludebyattribute:*.SkipCodeCoverage*;*CompilerGenerated*" \
		-output:opencovertests.xml

	$(NUGET_PACKAGEES)\ReportGenerator\$(REPORTGENERATOR)\tools\net47\ReportGenerator.exe \
		-reports:opencovertests.xml \
		-targetdir:coveragereport \
		-reporttypes:HTML

installer: $(INSTALLER_PACKAGE)
installer-snapshot: $(INSTALLER_SNAPSHOT) 
symbols: $(SYMBOLS_PACKAGE)
policies: $(POLICIES_PACKAGE)

#------------------------------------------------------------------------------
# Kokoro specific targets
#------------------------------------------------------------------------------

kokoro-continuous-integration: \
	check-copyright-headers \
	installer \
	installer-snapshot \
	symbols \
    policies \
	test

kokoro-sign: installer