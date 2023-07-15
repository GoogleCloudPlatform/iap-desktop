#
# Copyright 2020 Google LLC
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

vcpkg_from_github(
    OUT_SOURCE_PATH SOURCE_PATH
    REPO libssh2/libssh2
    REF libssh2-1.11.0
    SHA512 8ae38d76e7fae82843c7c6990760ad5827cb55e2aba096f684b832016614d76983d1871ae0857ba28efcb9fd65018ae0b34420f6841da6bb6c6ccebd9689ec0f
    HEAD_REF master
)

# Read build number defined by makefile
file(READ "${CMAKE_CURRENT_LIST_DIR}/build.tmp" LIBSSH2_BUILD)
file(READ "${CMAKE_CURRENT_LIST_DIR}/build-comma.tmp" LIBSSH2_BUILD_COMMA)

# Patch resource file to use custom build numbers
vcpkg_replace_string(${SOURCE_PATH}/src/libssh2.rc "#define RC_VERSION" "//#define UNUSED_VERSION")
vcpkg_replace_string(${SOURCE_PATH}/src/libssh2.rc "RC_VERSION" "${LIBSSH2_BUILD_COMMA}")
vcpkg_replace_string(${SOURCE_PATH}/src/libssh2.rc "LIBSSH2_VERSION" "\"${LIBSSH2_BUILD}\"")

# Patch resource file to embed OpenSSL version number
vcpkg_replace_string(${SOURCE_PATH}/src/libssh2.rc "#include <winver.h>" "#include <winver.h>\n#include <openssl/opensslv.h>")
vcpkg_replace_string(${SOURCE_PATH}/src/libssh2.rc "libssh2 Shared Library" "libssh2 with \" OPENSSL_VERSION_TEXT \"")

vcpkg_cmake_configure(
    SOURCE_PATH ${SOURCE_PATH}
    OPTIONS
        -DBUILD_EXAMPLES=OFF
        -DBUILD_TESTING=OFF
        -DENABLE_ZLIB_COMPRESSION=OFF
        -DCMAKE_CXX_FLAGS_RELEASE=/MT
        -DCMAKE_C_FLAGS_RELEASE=/MT
    OPTIONS_RELEASE
        -DENABLE_DEBUG_LOGGING=ON
)

vcpkg_cmake_install()
vcpkg_copy_pdbs()

vcpkg_cmake_config_fixup(CONFIG_PATH lib/cmake/libssh2)

if (VCPKG_TARGET_IS_WINDOWS)
    vcpkg_replace_string("${CURRENT_PACKAGES_DIR}/include/libssh2.h" "ifdef LIBSSH2_WIN32" "if 1")
    if (VCPKG_LIBRARY_LINKAGE STREQUAL "dynamic")
        vcpkg_replace_string("${CURRENT_PACKAGES_DIR}/include/libssh2.h" "ifdef _WINDLL" "if 1")
    else()
        vcpkg_replace_string("${CURRENT_PACKAGES_DIR}/include/libssh2.h" "ifdef _WINDLL" "if 0")
    endif()
endif()

file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/debug/include")
file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/debug/share")
# Do not delete the entire share directory as it contains the *-config.cmake files
file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/share/doc")
file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/share/man")


file(INSTALL "${SOURCE_PATH}/COPYING" DESTINATION "${CURRENT_PACKAGES_DIR}/share/${PORT}" RENAME copyright)
