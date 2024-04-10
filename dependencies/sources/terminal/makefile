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

# The tag should be increased whenever one of the dependencies is changed
TAG = 1

#
# Use the pre-built and signed Microsoft binaries.
#
TERMINAL_VERSION = 1.19.10821
TERMINAL_TAG = 1.19.10821.0
TERMINAL_PACKAGE_X86=https://github.com/microsoft/terminal/releases/download/v$(TERMINAL_TAG)/Microsoft.WindowsTerminal_$(TERMINAL_TAG)_x86.zip
TERMINAL_PACKAGE_X64=https://github.com/microsoft/terminal/releases/download/v$(TERMINAL_TAG)/Microsoft.WindowsTerminal_$(TERMINAL_TAG)_x64.zip

TERMINAL_PACKAGE_ID = Google.Solutions.ThirdParty.Terminal

POWERSHELL = powershell -NoProfile -ExecutionPolicy Unrestricted
SCRIPTSDIR=$(MAKEDIR)\..\..\..\sources\scripts
NUGET=nuget 

default: package

$(MAKEDIR)\obj\x86:
	@echo "========================================================"
	@echo "=== Downloading terminal (x86)                       ==="
	@echo "========================================================"

	if not exist $(MAKEDIR)\obj\x86 \
		md $(MAKEDIR)\obj\x86
	cd $(MAKEDIR)\obj
	
	curl -L --output x86.zip "$(TERMINAL_PACKAGE_X86)" 
	$(POWERSHELL) $(SCRIPTSDIR)\expand-zip.ps1 -ZipFile x86.zip $(MAKEDIR)\obj\x86
	
	cd $(MAKEDIR)

$(MAKEDIR)\obj\x64:
	@echo "========================================================"
	@echo "=== Downloading terminal (x64)                       ==="
	@echo "========================================================"

	if not exist $(MAKEDIR)\obj\x64 \
		md $(MAKEDIR)\obj\x64
	cd $(MAKEDIR)\obj
	
	curl -L --output x64.zip "$(TERMINAL_PACKAGE_X64)" 
	$(POWERSHELL) $(SCRIPTSDIR)\expand-zip.ps1 -ZipFile x64.zip $(MAKEDIR)\obj\x64
	
	cd $(MAKEDIR)

$(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).$(TERMINAL_VERSION).nupkg: \
		$(MAKEDIR)\obj\x86 \
		$(MAKEDIR)\obj\x64
	@echo "========================================================"
	@echo "=== Building terminal nuget package                   ==="
	@echo "========================================================"
	$(NUGET) pack -OutputDirectory $(MAKEDIR)\obj\ <<terminal.nuspec
<?xml version="1.0"?>
<package>
  <metadata>
    <id>$(TERMINAL_PACKAGE_ID)</id>
    <version>$(TERMINAL_VERSION)</version>
    <authors>https://github.com/microsoft/terminal</authors>
    <owners>https://github.com/microsoft/terminal</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Windows Terminal</description>
	<tags>Native, native</tags>
  </metadata>
  <files>
	<!-- pretend the library is platform-neutral -->
    <file src="$(MAKEDIR)\obj\x86\terminal-$(TERMINAL_TAG)\Microsoft.Terminal.Control.dll" target="runtimes\win10-x86\native" />
    <file src="$(MAKEDIR)\obj\x64\terminal-$(TERMINAL_TAG)\Microsoft.Terminal.Control.dll" target="runtimes\win10-x64\native" />
    <file src="$(TERMINAL_PACKAGE_ID).targets" target="build" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).$(TERMINAL_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).$(TERMINAL_VERSION).nupkg $(MAKEDIR)\obj\$(TERMINAL_PACKAGE_ID).nupkg

clean distclean:
    -rd /S /Q $(MAKEDIR)\obj
