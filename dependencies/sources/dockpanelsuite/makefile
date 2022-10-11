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
TAG = 2

DOCKPANELSUITE_TAG = Release_3.1.0
DOCKPANELSUITE_URL = https://github.com/dockpanelsuite/dockpanelsuite.git
DOCKPANELSUITE_VERSION = 3.1.0.$(TAG)

MSBUILD_PROPERTIES=Configuration=$(CONFIGURATION);Platform=Any CPU;TargetFrameworks=net40;TargetFrameworkVersion=4.0;PackageVersion=$(DOCKPANELSUITE_VERSION);OutputPath=bin\$(CONFIGURATION)

default: package

$(MAKEDIR)\obj\WinFormsUI\WinFormsUI.csproj:
	@echo "========================================================"
	@echo "=== Checking out dockpanelsuite                      ==="
	@echo "========================================================"

	git clone $(DOCKPANELSUITE_URL) $(MAKEDIR)\obj

	cd $(MAKEDIR)\obj

	git checkout $(DOCKPANELSUITE_TAG)

	git config user.email "iap-desktop+build@google.com"
	git config user.name "IAP Desktop Build"

	cd $(MAKEDIR)

$(MAKEDIR)\obj\WinFormsUI\bin\$(CONFIGURATION)\dockpanelsuite.$(DOCKPANELSUITE_VERSION).nupkg: \
		$(MAKEDIR)\obj\WinFormsUI\WinFormsUI.csproj
	@echo "========================================================"
	@echo "=== Building dockpanelsuite                          ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj

	msbuild \
		/t:Restore;Build \
		"/p:$(MSBUILD_PROPERTIES)" \
		"$(MAKEDIR)\obj\WinFormsUI\WinFormsUI.csproj"

	cd $(MAKEDIR)

$(MAKEDIR)\obj\WinFormsUI\bin\$(CONFIGURATION)\dockpanelsuite.ThemeVS2015.$(DOCKPANELSUITE_VERSION).nupkg: \
		$(MAKEDIR)\obj\WinFormsUI\ThemeVS2015.csproj
	@echo "========================================================"
	@echo "=== Building dockpanelsuite theme                    ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj

	msbuild \
		/t:Restore;Build \
		"/p:$(MSBUILD_PROPERTIES)" \
		"$(MAKEDIR)\obj\WinFormsUI\ThemeVS2015.csproj"

	cd $(MAKEDIR)

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: \
    $(MAKEDIR)\obj\WinFormsUI\bin\$(CONFIGURATION)\dockpanelsuite.$(DOCKPANELSUITE_VERSION).nupkg \
    $(MAKEDIR)\obj\WinFormsUI\bin\$(CONFIGURATION)\dockpanelsuite.ThemeVS2015.$(DOCKPANELSUITE_VERSION).nupkg
    
    copy /Y $(MAKEDIR)\obj\WinFormsUI\bin\$(CONFIGURATION)\dockpanelsuite.$(DOCKPANELSUITE_VERSION).nupkg $(MAKEDIR)\obj\dockpanelsuite.nupkg
    copy /Y $(MAKEDIR)\obj\WinFormsUI\bin\$(CONFIGURATION)\dockpanelsuite.ThemeVS2015.$(DOCKPANELSUITE_VERSION).nupkg $(MAKEDIR)\obj\dockpanelsuite.ThemeVS2015.nupkg

clean:
	msbuild \
		/t:Clean \
		"/p:$(MSBUILD_PROPERTIES)" \
		"$(MAKEDIR)\obj\WinFormsUI\WinFormsUI.csproj"
	msbuild \
		/t:Clean \
		"/p:$(MSBUILD_PROPERTIES)" \
		"$(MAKEDIR)\obj\WinFormsUI\ThemeVS2015.csproj"
    
    -rd /S /Q $(MAKEDIR)\obj\dockpanelsuite\bin
    -del $(MAKEDIR)\obj\dockpanelsuite.nupkg
    
force-clean:
    -rd /S /Q $(MAKEDIR)\obj