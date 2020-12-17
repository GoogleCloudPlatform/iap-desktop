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

include(vcpkg_common_functions)

vcpkg_from_github(
    OUT_SOURCE_PATH SOURCE_PATH
    REPO libssh2/libssh2

    # Build 1.9.0
    REF 42d37aa63129a1b2644bf6495198923534322d64
    SHA512 e86c0787e2aa7be5e9f19356e543493e53c7d1b51b585c46facfb05f769e6491209f820b207bf594348f4760c492c32dda3fcc94fc0af93cb09c736492a8e231

    # Build Ref with WinCNG fixes
    #    REF 6c7769dcc422250d14af1b06fce378b6ee009440
    #    SHA512 fa34c598149d28b12f5cefbee4816f30a807a1bde89faa3be469f690057cf2ea7dd1a83191b2a2cae3794e307d676efebd7a31d70d9587e42e0926f82a1ae73d

    HEAD_REF master
)

vcpkg_configure_cmake(
    SOURCE_PATH ${SOURCE_PATH}
    OPTIONS
        -DBUILD_EXAMPLES=OFF
        -DBUILD_TESTING=OFF
        -DENABLE_ZLIB_COMPRESSION=OFF
        -DDENABLE_DEBUG_LOGGING=ON
        -DCMAKE_CXX_FLAGS_RELEASE=/MT
        -DCMAKE_C_FLAGS_RELEASE=/MT
#        -DCRYPTO_BACKEND=WinCNG
)

vcpkg_install_cmake()

file(REMOVE_RECURSE ${CURRENT_PACKAGES_DIR}/debug/include)
file(REMOVE_RECURSE ${CURRENT_PACKAGES_DIR}/debug/lib/pkgconfig)
file(REMOVE_RECURSE ${CURRENT_PACKAGES_DIR}/debug/share)
file(REMOVE_RECURSE ${CURRENT_PACKAGES_DIR}/lib/pkgconfig)
file(REMOVE_RECURSE ${CURRENT_PACKAGES_DIR}/share)

vcpkg_fixup_cmake_targets(CONFIG_PATH lib/cmake/libssh2)

file(INSTALL ${CMAKE_CURRENT_LIST_DIR}/LICENSE DESTINATION ${CURRENT_PACKAGES_DIR}/share/libssh2 RENAME copyright)

vcpkg_copy_pdbs()
