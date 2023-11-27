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
TAG = 33

#
# NB. We're using custom ports for libssh2 and openssl:
#
#  libssh2 port: Uses custom build flags and embeds version number
#  openssl port: Uses 1.1.1 instead of 3.0 
#                (vcpkg migrated from 1.1.1 to 3.0 in 22Q2)
#
# To update the versions of ports, edit:
#
#  vcpkg.json: Bump 'port-version', update 'version'
#  portfile.cmake: Update version/tag, file hash
#
#
VCPKG_TAG = 2023.06.20
VCPKG_URL = https://github.com/microsoft/vcpkg.git
LIBSSH2_VERSION = 1.11.0.$(TAG)
LIBSSH2_PACKAGE_ID = Google.Solutions.ThirdParty.Libssh2

default: package

#------------------------------------------------------------------------------
# vcpkg
#------------------------------------------------------------------------------

$(MAKEDIR)\obj\vcpkg\bootstrap-vcpkg.bat:
	@echo "========================================================"
	@echo "=== Checking out vcpkg                               ==="
	@echo "========================================================"

	git clone $(VCPKG_URL) $(MAKEDIR)\obj\vcpkg

$(MAKEDIR)\obj\vcpkg\vcpkg.exe: $(MAKEDIR)\obj\vcpkg\bootstrap-vcpkg.bat
	@echo "========================================================"
	@echo "=== Building vcpkg                                   ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj\vcpkg
	git checkout $(VCPKG_TAG)
	cd $(MAKEDIR)
	
	$(MAKEDIR)\obj\vcpkg\bootstrap-vcpkg.bat

#------------------------------------------------------------------------------
# libssh2
#------------------------------------------------------------------------------

$(MAKEDIR)\obj\vcpkg\installed\libssh2-x86-windows-mixed\bin\libssh2.dll: $(MAKEDIR)\obj\vcpkg\vcpkg.exe
	@echo "========================================================"
	@echo "=== Building libssh2 (x86)                           ==="
	@echo "========================================================"

	-echo | set /p="$(LIBSSH2_VERSION)" > $(MAKEDIR)\vcpkg-ports\libssh2\build.tmp
	-echo | set /p="$(LIBSSH2_VERSION:.=,)" > $(MAKEDIR)\vcpkg-ports\libssh2\build-comma.tmp

	$(MAKEDIR)\obj\vcpkg\vcpkg.exe install libssh2 \
		--triplet libssh2-x86-windows-mixed \
		--overlay-ports=$(MAKEDIR)\vcpkg-ports \
		--overlay-triplets=$(MAKEDIR)\vcpkg-triplets

$(MAKEDIR)\obj\vcpkg\installed\libssh2-x64-windows-mixed\bin\libssh2.dll: $(MAKEDIR)\obj\vcpkg\vcpkg.exe
	@echo "========================================================"
	@echo "=== Building libssh2 (x64)                           ==="
	@echo "========================================================"

	-echo | set /p="$(LIBSSH2_VERSION)" > $(MAKEDIR)\vcpkg-ports\libssh2\build.tmp
	-echo | set /p="$(LIBSSH2_VERSION:.=,)" > $(MAKEDIR)\vcpkg-ports\libssh2\build-comma.tmp

	$(MAKEDIR)\obj\vcpkg\vcpkg.exe install libssh2 \
		--triplet libssh2-x64-windows-mixed \
		--overlay-ports=$(MAKEDIR)\vcpkg-ports \
		--overlay-triplets=$(MAKEDIR)\vcpkg-triplets

$(MAKEDIR)\obj\libssh2\$(LIBSSH2_PACKAGE_ID).$(LIBSSH2_VERSION).nupkg: \
		$(MAKEDIR)\obj\vcpkg\installed\libssh2-x86-windows-mixed\bin\libssh2.dll \
		$(MAKEDIR)\obj\vcpkg\installed\libssh2-x64-windows-mixed\bin\libssh2.dll
	@echo "========================================================"
	@echo "=== Building libssh2 nuget package                   ==="
	@echo "========================================================"
	nuget pack -OutputDirectory $(MAKEDIR)\obj\libssh2\ <<libssh2.nuspec
<?xml version="1.0"?>
<package>
  <metadata>
    <id>$(LIBSSH2_PACKAGE_ID)</id>
    <version>$(LIBSSH2_VERSION)</version>
    <authors>https://www.libssh2.org/</authors>
    <owners>https://www.libssh2.org/</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>libssh2 is a client-side C library implementing the SSH2 protocol</description>
	<tags>Native, native</tags>
  </metadata>
  <files>
	<!-- pretend the library is platform-neutral -->
    <file src="obj\vcpkg\installed\libssh2-x86-windows-mixed\bin\libssh2.dll" target="runtimes\win10-x86\native" />
    <file src="obj\vcpkg\installed\libssh2-x86-windows-mixed\bin\libssh2.pdb" target="runtimes\win10-x86\native" />
    <file src="obj\vcpkg\installed\libssh2-x64-windows-mixed\bin\libssh2.dll" target="runtimes\win10-x64\native" />
    <file src="obj\vcpkg\installed\libssh2-x64-windows-mixed\bin\libssh2.pdb" target="runtimes\win10-x64\native" />
    <file src="libssh2.targets" target="build" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\libssh2\$(LIBSSH2_PACKAGE_ID).$(LIBSSH2_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\libssh2\$(LIBSSH2_PACKAGE_ID).$(LIBSSH2_VERSION).nupkg $(MAKEDIR)\obj\$(LIBSSH2_PACKAGE_ID).nupkg

clean: $(MAKEDIR)\obj\vcpkg\vcpkg.exe
	@echo "========================================================"
	@echo "=== Cleaning libssh2                                 ==="
	@echo "========================================================"
	$(MAKEDIR)\obj\vcpkg\vcpkg.exe remove openssl libssh2 \
		--triplet libssh2-x86-windows-mixed \
		--overlay-ports=$(MAKEDIR)\vcpkg-ports \
		--overlay-triplets=$(MAKEDIR)\vcpkg-triplets
	$(MAKEDIR)\obj\vcpkg\vcpkg.exe remove openssl libssh2 \
		--triplet libssh2-x64-windows-mixed \
		--overlay-ports=$(MAKEDIR)\vcpkg-ports \
		--overlay-triplets=$(MAKEDIR)\vcpkg-triplets
	-rd /S /Q $(MAKEDIR)\obj\libssh2\
    -rd /S /Q $(LOCALAPPDATA)\vcpkg\archives
    -del $(MAKEDIR)\obj\$(LIBSSH2_PACKAGE_ID).nupkg

distclean: clean
    -rd /S /Q $(MAKEDIR)\obj