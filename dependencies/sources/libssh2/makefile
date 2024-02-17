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
PLATFORM = x86

LIBSSH2_REF = feature/wincng-ecdsa-pr
LIBSSH2_URL = https://github.com/jpassing/fork-libssh2.git

LIBSSH2_OPTIONS = \
	-DBUILD_STATIC_LIBS=OFF \
	-DBUILD_SHARED_LIBS=ON \
	-DCRYPTO_BACKEND=WinCNG \
	-DBUILD_EXAMPLES=OFF \
	-DBUILD_TESTING=OFF \
	-DENABLE_ZLIB_COMPRESSION=OFF \
	-DCMAKE_C_FLAGS_RELEASE=/MT \
	-DCMAKE_C_FLAGS_DEBUG=/MTd \
	-DENABLE_DEBUG_LOGGING=ON

!if ("$(PLATFORM)" == "x86")
CMAKE_PLATFORM = Win32
!else
CMAKE_PLATFORM = $(PLATFORM)
!endif

CMAKE_GENERATOR = "Visual Studio 17 2022"
CMAKE_OPTS = -Wno-dev -DCMAKE_BUILD_TYPE=$(CONFIGURATION) -A $(CMAKE_PLATFORM)
CMAKE_BUILDDIR = build\$(PLATFORM)

# The tag should be increased whenever one of the dependencies is changed
TAG = 100

#------------------------------------------------------------------------------
# Compile targets
#------------------------------------------------------------------------------

default: $(MAKEDIR)\obj\$(CMAKE_BUILDDIR)\src\$(PLATFORM)\libssh2.dll

$(MAKEDIR)\obj\src:
	@echo "========================================================"
	@echo "=== Checking out libssh2                             ==="
	@echo "========================================================"

	git clone $(LIBSSH2_URL) $(MAKEDIR)\obj
	cd $(MAKEDIR)\obj
	git checkout $(LIBSSH2_REF)

$(MAKEDIR)\obj\$(CMAKE_BUILDDIR)\src\$(PLATFORM)\libssh2.dll: $(MAKEDIR)\obj\src
	@echo "========================================================"
	@echo "=== Building libssh2                                 ==="
	@echo "========================================================"
	
	cd $(MAKEDIR)\obj
	
	powershell -Command "(Get-Content src\libssh2.rc) \
		-replace 'The libssh2 library\\0','libssh2, built for IAP Desktop' | \
		Out-File src\libssh2.rc -Encoding ascii"
	powershell -Command "(Get-Content src\libssh2.rc) \
		-replace 'LIBSSH2_VERSION_PATCH','$(TAG)' | \
		Out-File src\libssh2.rc -Encoding ascii"
	
	cmake \
		$(CMAKE_OPTS) \
		-G $(CMAKE_GENERATOR) \
		$(LIBSSH2_OPTIONS) \
		-B $(CMAKE_BUILDDIR)
	
	cmake --build $(CMAKE_BUILDDIR) --config $(CONFIGURATION)


#------------------------------------------------------------------------------
# Package targets
#------------------------------------------------------------------------------

#------------------------------------------------------------------------------
# Clean targets
#------------------------------------------------------------------------------

clean:
	cd $(MAKEDIR)\obj
	cmake --build $(CMAKE_BUILDDIR) --target clean

distclean:
    -rd /S /Q $(MAKEDIR)\obj
	
	
	
	
# TODO: Create nupkg	

