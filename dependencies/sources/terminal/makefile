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

TERMINAL_TAG = v1.16.2641.0
TERMINAL_URL = https://github.com/microsoft/terminal.git
TERMINAL_VERSION = 1.16.2641.$(TAG)

NUGET = $(MAKEDIR)\obj\dep\nuget\nuget.exe
TERMINAL_VCXPROJ=$(MAKEDIR)\obj\src\cascadia\PublicTerminalCore\PublicTerminalCore.vcxproj

default: package

$(TERMINAL_VCXPROJ):
	@echo "========================================================"
	@echo "=== Checking out terminal                            ==="
	@echo "========================================================"

	git clone $(TERMINAL_URL) $(MAKEDIR)\obj

	cd $(MAKEDIR)\obj

	git checkout tags/$(TERMINAL_TAG)
	git submodule update --init --recursive

	git config user.email "iap-desktop+build@google.com"
	git config user.name "IAP Desktop Build"
	
	git am --reject --whitespace=fix $(MAKEDIR)\patches\0001-Change-target-to-terminal.dll.patch

	$(NUGET) restore $(MAKEDIR)\obj\OpenConsole.sln
	$(NUGET) restore $(MAKEDIR)\obj\dep\nuget\packages.config

	cd $(MAKEDIR)

$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\terminal.dll: \
		$(TERMINAL_VCXPROJ)
	@echo "========================================================"
	@echo "=== Building terminal (x86)                          ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj
	
	msbuild \
		/t:Build \
		"/p:Configuration=$(CONFIGURATION);SolutionDir=$(MAKEDIR)\obj\,Platform=Win32" \
		$**

	cd $(MAKEDIR)

$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\terminal.dll: \
		$(TERMINAL_VCXPROJ)
	@echo "========================================================"
	@echo "=== Building terminal (x64)                          ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj
	
	msbuild \
		/t:Build \
		"/p:Configuration=$(CONFIGURATION);SolutionDir=$(MAKEDIR)\obj\,Platform=x64" \
		$**

	cd $(MAKEDIR)

$(MAKEDIR)\obj\terminal.$(TERMINAL_VERSION).nupkg: \
		$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\terminal.dll \
		$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\terminal.dll
	@echo "========================================================"
	@echo "=== Building terminal nuget package                   ==="
	@echo "========================================================"
	$(NUGET) pack -OutputDirectory $(MAKEDIR)\obj\ <<terminal.nuspec
<?xml version="1.0"?>
<package>
  <metadata>
    <id>terminal</id>
    <version>$(TERMINAL_VERSION)</version>
    <authors>https://github.com/microsoft/terminal</authors>
    <owners>https://github.com/microsoft/terminal</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Windows Terminal</description>
	<tags>Native, native</tags>
  </metadata>
  <files>
	<!-- pretend the library is platform-neutral -->
    <file src="$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\terminal.dll" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\bin\Win32\$(CONFIGURATION)\terminal.pdb" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\terminal.dll" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\bin\x64\$(CONFIGURATION)\terminal.pdb" target="runtimes\win10-x86\native" />
    <file src="terminal.targets" target="build" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\terminal.$(TERMINAL_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\terminal.$(TERMINAL_VERSION).nupkg $(MAKEDIR)\obj\terminal.nupkg

clean:
    msbuild /t:Clean "$(TERMINAL_VCXPROJ)"
    -rd /S /Q $(MAKEDIR)\obj\bin
    -del $(MAKEDIR)\obj\terminal.nupkg
    
force-clean:
    -rd /S /Q $(MAKEDIR)\obj

#debug: clean package
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x86\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x86\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x86\$(CONFIGURATION)
#                                                                                  
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x86\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x86\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x86\$(CONFIGURATION)
#                                                                                  
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x64\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x64\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x64\$(CONFIGURATION)
#                                                                                  
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x64\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x64\$(CONFIGURATION)
#    copy /Y $(MAKEDIR)\obj\bin\$(CONFIGURATION)\terminal.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x64\$(CONFIGURATION)
#