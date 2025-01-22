#
# Copyright 2024 Google LLC
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
CONFIGURATION = Release
PLATFORM = x86

# The tag should be increased whenever one of the dependencies is changed
TAG = 3

ICUSHIM_VERSION=1.21.3231.$(TAG)
ICUSHIM_PACKAGE_ID = Google.Solutions.IcuShim

!if ("$(PLATFORM)" == "x86")
VC_PLATFORM = Win32
!else
VC_PLATFORM = $(PLATFORM)
!endif

#------------------------------------------------------------------------------
# Compile targets
#------------------------------------------------------------------------------

default: package

$(MAKEDIR)\bin\$(PLATFORM)\$(CONFIGURATION)\icu.dll:
	@echo "========================================================"
	@echo "=== Building icu ($(PLATFORM))                               ==="
	@echo "========================================================"
	
	msbuild /p:Configuration=$(CONFIGURATION);Platform=$(PLATFORM);BuildNumber=$(TAG) icu.sln

compile: $(MAKEDIR)\bin\$(PLATFORM)\$(CONFIGURATION)\icu.dll

#------------------------------------------------------------------------------
# Package targets
#------------------------------------------------------------------------------

$(MAKEDIR)\obj\$(ICUSHIM_PACKAGE_ID).$(ICUSHIM_VERSION).nupkg:
	$(MAKE) PLATFORM=x86 CONFIGURATION=$(CONFIGURATION) compile
	$(MAKE) PLATFORM=x64 CONFIGURATION=$(CONFIGURATION) compile
	$(MAKE) PLATFORM=arm64 CONFIGURATION=$(CONFIGURATION) compile

	@echo "========================================================"
	@echo "=== Building icushim nuget package                   ==="
	@echo "========================================================"
	nuget pack -OutputDirectory $(MAKEDIR)\obj\ <<libssh2.nuspec
<?xml version="1.0"?>
<package>
  <metadata>
    <id>$(ICUSHIM_PACKAGE_ID)</id>
    <version>$(ICUSHIM_VERSION)</version>
    <authors>Google LLC</authors>
    <owners>Google LLC</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>ICU Shim for Microsoft Terminal</description>
	<tags>Native, native</tags>
  </metadata>
  <files>
	<!-- pretend the library is platform-neutral -->
    <file src="$(MAKEDIR)\bin\Win32\$(CONFIGURATION)\icu.dll" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\bin\Win32\$(CONFIGURATION)\icu.pdb" target="runtimes\win10-x86\native" />

    <file src="$(MAKEDIR)\bin\x64\$(CONFIGURATION)\icu.dll" target="runtimes\win10-x64\native" />
    <file src="$(MAKEDIR)\bin\x64\$(CONFIGURATION)\icu.pdb" target="runtimes\win10-x64\native" />

    <file src="$(MAKEDIR)\bin\arm64\$(CONFIGURATION)\icu.dll" target="runtimes\win10-arm64\native" />
    <file src="$(MAKEDIR)\bin\arm64\$(CONFIGURATION)\icu.pdb" target="runtimes\win10-arm64\native" />
    <file src="$(ICUSHIM_PACKAGE_ID).targets" target="build" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\$(ICUSHIM_PACKAGE_ID).$(ICUSHIM_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\$(ICUSHIM_PACKAGE_ID).$(ICUSHIM_VERSION).nupkg $(MAKEDIR)\obj\$(ICUSHIM_PACKAGE_ID).nupkg

clean:
	-del *.nupkg
	msbuild -t:clean icu.sln

distclean: clean
    -rd /S /Q $(MAKEDIR)\obj
    -rd /S /Q $(MAKEDIR)\bin
