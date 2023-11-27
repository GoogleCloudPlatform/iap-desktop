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
TAG = 31

VTNETCORE_TAG = 9e68f5561dc52edb780615b3fe133289216b3dba
VTNETCORE_URL = https://github.com/darrenstarr/VtNetCore.git
VTNETCORE_VERSION = 1.0.30.$(TAG)
VTNETCORE_PACKAGE_ID = Google.Solutions.ThirdParty.Vtnetcore

default: package

$(MAKEDIR)\obj\VtNetCore\VtNetCore.csproj:
	@echo "========================================================"
	@echo "=== Checking out vtnetcore                           ==="
	@echo "========================================================"

	git clone $(VTNETCORE_URL) $(MAKEDIR)\obj

	cd $(MAKEDIR)\obj

	git checkout $(VTNETCORE_TAG)

	git config user.email "iap-desktop+build@google.com"
	git config user.name "IAP Desktop Build"
	git am $(MAKEDIR)\patches\0001-Handle-OSC-sequences-with-empty-text-parameters.patch
	git am $(MAKEDIR)\patches\0002-Fix-escape-sequences-for-bracketed-text-mode.patch
	git am $(MAKEDIR)\patches\0003-Detect-key-modifier-options-XTMODKEYS.patch
	git am $(MAKEDIR)\patches\0004-Handle-single-parameter-OSC-sequences.patch
	git am $(MAKEDIR)\patches\0005-Fix-throw-statement-to-avoid-cascading-exception.patch
	git am $(MAKEDIR)\patches\0006-Use-TraceSource-for-logging.patch    
    git am $(MAKEDIR)\patches\0007-Include-symbols-in-NuGet-package.patch
    git am $(MAKEDIR)\patches\0008-Handle-xterm-input-errors-more-gracefully.patch
    git am $(MAKEDIR)\patches\0009-Propagate-terminal-attributes.patch
    git am $(MAKEDIR)\patches\0010-Allow-custom-default-attributes.patch
	git am $(MAKEDIR)\patches\0011-Expose-properties-for-back-foreground-color.patch
    git am $(MAKEDIR)\patches\0012-Add-missing-ISO-8613-3-color-mappings.patch
    git am $(MAKEDIR)\patches\0013-Change-TerminalColor-class-to-be-readonly.patch
    git am $(MAKEDIR)\patches\0014-Fix-Restore-Cursor-command.patch

	cd $(MAKEDIR)

$(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\$(VTNETCORE_PACKAGE_ID).$(VTNETCORE_VERSION).nupkg: \
		$(MAKEDIR)\obj\VtNetCore\VtNetCore.csproj
	@echo "========================================================"
	@echo "=== Building vtnetcore                               ==="
	@echo "========================================================"
	cd $(MAKEDIR)\obj

	msbuild \
		/t:Restore;Build \
		"/p:Configuration=$(CONFIGURATION);Platform=Any CPU;AssemblyName=vtnetcore;PackageId=$(VTNETCORE_PACKAGE_ID);Version=$(VTNETCORE_VERSION);OutputPath=bin\$(CONFIGURATION)" \
		"$(MAKEDIR)\obj\VtNetCore\VtNetCore.csproj"

	cd $(MAKEDIR)

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\$(VTNETCORE_PACKAGE_ID).$(VTNETCORE_VERSION).nupkg 
    copy /Y \
		$(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\$(VTNETCORE_PACKAGE_ID).$(VTNETCORE_VERSION).nupkg \
		$(MAKEDIR)\obj\$(VTNETCORE_PACKAGE_ID).nupkg

clean:
    msbuild /t:Clean "$(MAKEDIR)\obj\VtNetCore\VtNetCore.csproj"
    -rd /S /Q $(MAKEDIR)\obj\VtNetCore\bin
    -del $(MAKEDIR)\obj\$(VTNETCORE_PACKAGE_ID).nupkg
    
distclean:
    -rd /S /Q $(MAKEDIR)\obj

debug: clean package
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x86\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x86\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x86\$(CONFIGURATION)
                                                                                  
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x86\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x86\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x86\$(CONFIGURATION)
                                                                                  
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x64\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x64\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.dll $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x64\$(CONFIGURATION)
                                                                                  
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell\bin\x64\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop.Extensions.Shell.Test\bin\x64\$(CONFIGURATION)
    copy /Y $(MAKEDIR)\obj\VtNetCore\bin\$(CONFIGURATION)\vtnetcore.pdb $(MAKEDIR)\..\..\..\sources\Google.Solutions.IapDesktop\bin\x64\$(CONFIGURATION)
