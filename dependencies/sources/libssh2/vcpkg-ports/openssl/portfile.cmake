if(EXISTS "${CURRENT_INSTALLED_DIR}/include/openssl/ssl.h")
    message(FATAL_ERROR "Can't build openssl if libressl/boringssl is installed. Please remove libressl/boringssl, and try install openssl again if you need it.")
endif()

set(OPENSSL_VERSION 1.1.1s)
vcpkg_download_distfile(
    ARCHIVE
    URLS "https://www.openssl.org/source/openssl-${OPENSSL_VERSION}.tar.gz" "https://www.openssl.org/source/openssl-${OPENSSL_VERSION}.tar.gz"
    FILENAME "openssl-${OPENSSL_VERSION}.tar.gz"
    SHA512 2ef983f166b5e1bf456ca37938e7e39d58d4cd85e9fc4b5174a05f5c37cc5ad89c3a9af97a6919bcaab128a8a92e4bdc8a045e5d9156d90768da8f73ac67c5b9
)

vcpkg_find_acquire_program(PERL)
get_filename_component(PERL_EXE_PATH ${PERL} DIRECTORY)
vcpkg_add_to_path("${PERL_EXE_PATH}")

if(VCPKG_TARGET_IS_UWP)
    include("${CMAKE_CURRENT_LIST_DIR}/uwp/portfile.cmake")
    include("${CMAKE_CURRENT_LIST_DIR}/install-pc-files.cmake")
elseif(VCPKG_TARGET_IS_WINDOWS AND NOT VCPKG_TARGET_IS_MINGW)
    include("${CMAKE_CURRENT_LIST_DIR}/windows/portfile.cmake")
    include("${CMAKE_CURRENT_LIST_DIR}/install-pc-files.cmake")
else()
    include("${CMAKE_CURRENT_LIST_DIR}/unix/portfile.cmake")
endif()

configure_file("${CMAKE_CURRENT_LIST_DIR}/vcpkg-cmake-wrapper.cmake.in" "${CURRENT_PACKAGES_DIR}/share/${PORT}/vcpkg-cmake-wrapper.cmake" @ONLY)
file(INSTALL "${CMAKE_CURRENT_LIST_DIR}/usage" DESTINATION "${CURRENT_PACKAGES_DIR}/share/${PORT}")
