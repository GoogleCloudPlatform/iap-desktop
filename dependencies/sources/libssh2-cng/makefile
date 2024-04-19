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
TAG = 100

LIBSSH2_REF = 9d195e950281812c4ef066e4c0b82cf488b028ff
LIBSSH2_URL = https://github.com/libssh2/libssh2.git

LIBSSH2_VERSION = 1.11.$(TAG)
LIBSSH2_PACKAGE_ID = Google.Solutions.ThirdParty.Libssh2.Cng

#
# NB. CMAKE_POLICY_DEFAULT_CMP0091=NEW is required, otherwise cmake 
#     silently ignores CMAKE_MSVC_RUNTIME_LIBRARY.
#

LIBSSH2_OPTIONS = \
	-DBUILD_STATIC_LIBS=OFF \
	-DBUILD_SHARED_LIBS=ON \
	-DCRYPTO_BACKEND=WinCNG \
	-DBUILD_EXAMPLES=OFF \
	-DBUILD_TESTING=OFF \
	-DENABLE_ZLIB_COMPRESSION=OFF \
	-DENABLE_ECDSA_WINCNG=ON \
	-DENABLE_DEBUG_LOGGING=ON \
	-DCMAKE_POLICY_DEFAULT_CMP0091=NEW

!if ("$(PLATFORM)" == "x86")
CMAKE_PLATFORM = Win32
!else
CMAKE_PLATFORM = $(PLATFORM)
!endif

!if ("$(CONFIGURATION)" == "Debug")
CMAKE_CONFIGURATION = Debug
LIBSSH2_OPTIONS = $(LIBSSH2_OPTIONS) -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreadedDebug
!else
CMAKE_CONFIGURATION = RelWithDebInfo
LIBSSH2_OPTIONS = $(LIBSSH2_OPTIONS) -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded
!endif

CMAKE = cmake
CMAKE_GENERATOR = "Visual Studio 17 2022"
CMAKE_OPTS = -Wno-dev -DCMAKE_BUILD_TYPE=$(CONFIGURATION) -A $(CMAKE_PLATFORM)
CMAKE_BUILDDIR = build\$(PLATFORM)

#------------------------------------------------------------------------------
# Compile targets
#------------------------------------------------------------------------------

default: package

$(MAKEDIR)\obj\src:
	@echo "========================================================"
	@echo "=== Checking out libssh2                             ==="
	@echo "========================================================"

	git clone $(LIBSSH2_URL) $(MAKEDIR)\obj
	cd $(MAKEDIR)\obj
	git checkout $(LIBSSH2_REF)

$(MAKEDIR)\obj\$(CMAKE_BUILDDIR)\src\$(CMAKE_CONFIGURATION)\libssh2.dll: $(MAKEDIR)\obj\src
	@echo "========================================================"
	@echo "=== Building libssh2 ($(PLATFORM))                           ==="
	@echo "========================================================"
	
	cd $(MAKEDIR)\obj
	
	powershell -Command "(Get-Content src\libssh2.rc) \
		-replace 'The libssh2 library\\0','libssh2, built for IAP Desktop' | \
		Out-File src\libssh2.rc -Encoding ascii"
	powershell -Command "(Get-Content src\libssh2.rc) \
		-replace 'LIBSSH2_VERSION_PATCH','$(TAG)' | \
		Out-File src\libssh2.rc -Encoding ascii"
	
	$(CMAKE) \
		$(CMAKE_OPTS) \
		-G $(CMAKE_GENERATOR) \
		$(LIBSSH2_OPTIONS) \
		-B $(CMAKE_BUILDDIR)
	
	$(CMAKE) --build $(CMAKE_BUILDDIR) --config $(CMAKE_CONFIGURATION)

compile: $(MAKEDIR)\obj\$(CMAKE_BUILDDIR)\src\$(CMAKE_CONFIGURATION)\libssh2.dll

#------------------------------------------------------------------------------
# Package targets
#------------------------------------------------------------------------------

$(MAKEDIR)\obj\$(LIBSSH2_PACKAGE_ID).$(LIBSSH2_VERSION).nupkg:
	$(MAKE) PLATFORM=x86 CONFIGURATION=$(CONFIGURATION) compile
	$(MAKE) PLATFORM=x64 CONFIGURATION=$(CONFIGURATION) compile

	@echo "========================================================"
	@echo "=== Building libssh2 nuget package                   ==="
	@echo "========================================================"
	nuget pack -OutputDirectory $(MAKEDIR)\obj\ <<libssh2.nuspec
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
    <file src="obj\build\x86\src\$(CMAKE_CONFIGURATION)\libssh2.dll" target="runtimes\win10-x86\native" />
    <file src="obj\build\x86\src\$(CMAKE_CONFIGURATION)\libssh2.pdb" target="runtimes\win10-x86\native" />
    <file src="obj\build\x64\src\$(CMAKE_CONFIGURATION)\libssh2.dll" target="runtimes\win10-x64\native" />
    <file src="obj\build\x64\src\$(CMAKE_CONFIGURATION)\libssh2.pdb" target="runtimes\win10-x64\native" />
    <file src="$(LIBSSH2_PACKAGE_ID).targets" target="build" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\$(LIBSSH2_PACKAGE_ID).$(LIBSSH2_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\$(LIBSSH2_PACKAGE_ID).$(LIBSSH2_VERSION).nupkg $(MAKEDIR)\obj\$(LIBSSH2_PACKAGE_ID).nupkg

clean:
	cd $(MAKEDIR)\obj
	-del *.nupkg
	$(CMAKE) --build build\x86 --target clean
	$(CMAKE) --build build\x64 --target clean

distclean:
    -rd /S /Q $(MAKEDIR)\obj
