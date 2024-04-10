#
# Copyright 2022 Google LLC
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

# The tag should be increased whenever one of the dependencies is changed
TAG = 1

TERMINAL_TAG = v1.20.10822.0
TERMINAL_URL = https://github.com/microsoft/terminal.git
TERMINAL_VERSION = 1.16.2641.$(TAG)
TERMINAL_PACKAGE_ID = Google.Solutions.ThirdParty.Terminal

NUGET = $(MAKEDIR)\obj\dep\nuget\nuget.exe
TERMINAL_VCXPROJ=$(MAKEDIR)\obj\src\cascadia\TerminalControl\dll\TerminalControl.vcxproj
HOSTPROXY_VCXPROJ=$(MAKEDIR)\obj\src\host\proxy\Host.Proxy.vcxproj

default: package

$(MAKEDIR)\obj:
	@echo "========================================================"
	@echo "=== Checking out terminal                            ==="
	@echo "========================================================"

	git clone $(TERMINAL_URL) $(MAKEDIR)\obj

	cd $(MAKEDIR)\obj

	git checkout tags/$(TERMINAL_TAG)
	git submodule update --init --recursive

#$(NUGET) restore $(MAKEDIR)\obj\OpenConsole.sln
#$(NUGET) restore $(MAKEDIR)\obj\dep\nuget\packages.config

	cd $(MAKEDIR)

$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.dll: $(MAKEDIR)\obj
	@echo "========================================================"
	@echo "=== Building terminal (x86)                          ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj
	
	msbuild \
		/t:Build \
		-restore \
		"/p:Configuration=$(CONFIGURATION);SolutionDir=$(MAKEDIR)\obj\,Platform=Win32" \
		$(HOSTPROXY_VCXPROJ)
	msbuild \
		/t:Build \
		-restore \
		"/p:Configuration=$(CONFIGURATION);SolutionDir=$(MAKEDIR)\obj\,Platform=Win32" \
		$(TERMINAL_VCXPROJ)

	cd $(MAKEDIR)

$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.dll: $(MAKEDIR)\obj
	@echo "========================================================"
	@echo "=== Building terminal (x64)                          ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj
	
	msbuild \
		/t:Build \
		-restore \
		"/p:Configuration=$(CONFIGURATION);SolutionDir=$(MAKEDIR)\obj\,Platform=x64" \
		$(HOSTPROXY_VCXPROJ)
	msbuild \
		/t:Build \
		-restore \
		"/p:Configuration=$(CONFIGURATION);SolutionDir=$(MAKEDIR)\obj\,Platform=x64" \
		$(TERMINAL_VCXPROJ)

	cd $(MAKEDIR)

$(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).$(TERMINAL_VERSION).nupkg: \
		$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.dll \
		$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.dll
	@echo "========================================================"
	@echo "=== Building terminal nuget package                   ==="
	@echo "========================================================"
	$(NUGET) pack -OutputDirectory $(MAKEDIR)\obj\ <<terminal.nuspec
<?xml version="1.0"?>
<package>
  <metadata>
    <id>Google.Solutions.ThirdParty.Terminal</id>
    <version>$(TERMINAL_VERSION)</version>
    <authors>https://github.com/microsoft/terminal</authors>
    <owners>https://github.com/microsoft/terminal</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Windows Terminal</description>
	<tags>Native, native</tags>
  </metadata>
  <files>
	<!-- pretend the library is platform-neutral -->
    <file src="$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.dll" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.pdb" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.dll" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\Microsoft.Terminal.Control\Microsoft.Terminal.Control.pdb" target="runtimes\win10-x86\native" />
    <file src="terminal.targets" target="build" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).$(TERMINAL_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).$(TERMINAL_VERSION).nupkg $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).nupkg

clean:
    msbuild /t:Clean "$(TERMINAL_VCXPROJ)"
    -rd /S /Q $(MAKEDIR)\obj\bin
    -del $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).nupkg
    
distclean:
    -rd /S /Q $(MAKEDIR)\obj
