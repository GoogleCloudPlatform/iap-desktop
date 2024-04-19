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

# GCS bucket used for storing results
PACKAGES_BUCKET = iapdesktop-kokoro-artifacts

# Directories containing projects
DIRS=\
    $(MAKEDIR)\sources\tsc \
    $(MAKEDIR)\sources\vtnetcore \
    $(MAKEDIR)\sources\dockpanelsuite \
    $(MAKEDIR)\sources\libssh2 \
    $(MAKEDIR)\sources\libssh2-cng \
    $(MAKEDIR)\sources\terminal

# Nuget packages to build
PACKAGES=$(PACKAGES) \
	$(MAKEDIR)\sources\tsc\obj\google.solutions.tsc.nupkg \
    $(MAKEDIR)\sources\vtnetcore\obj\Google.Solutions.ThirdParty.Vtnetcore.nupkg \
    $(MAKEDIR)\sources\dockpanelsuite\obj\Google.Solutions.ThirdParty.DockPanelSuite.nupkg \
    $(MAKEDIR)\sources\libssh2\obj\Google.Solutions.ThirdParty.Libssh2.nupkg \
    $(MAKEDIR)\sources\libssh2-cng\obj\Google.Solutions.ThirdParty.Libssh2.Cng.nupkg \
    $(MAKEDIR)\sources\terminal\obj\Google.Solutions.ThirdParty.Terminal.nupkg

default: publish

#------------------------------------------------------------------------------
# Common targets
#------------------------------------------------------------------------------

$(MAKEDIR)\NuGetPackages:
	@echo "========================================================"
	@echo "=== Creating nuget package repository                ===
	@echo "========================================================"
	md $(MAKEDIR)\NuGetPackages
	-nuget sources add -Name iap-desktop-dependencies -Source $(MAKEDIR)\NuGetPackages

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

$(PACKAGES): $(MAKEDIR)\NuGetPackages
    cd $(@D)\..
    $(MAKE) /NOLOGO CONFIGURATION=$(CONFIGURATION) package
    cd $(MAKEDIR)
    
package: $(PACKAGES)

publish: $(PACKAGES)
    !nuget add $** -Source $(MAKEDIR)\NuGetPackages

!if ( "$(KOKORO_BUILD_NUMBER)" != "" ) 
    gsutil cp -r NuGetPackages/* gs://$(PACKAGES_BUCKET)/NuGetPackages/
!endif

clean distclean: $(DIRS)
    !cd $** && \
        $(MAKE) /NOLOGO CONFIGURATION=$(CONFIGURATION) $@ && \
        cd $(MAKEDIR)

    -rd /S /Q $(MAKEDIR)\NuGetPackages
    -rd /S /Q $(MAKEDIR)\..\sources\packages\vtnetcore.*
    -rd /S /Q $(MAKEDIR)\..\sources\packages\libssh2.*