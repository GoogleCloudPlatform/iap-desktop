#
# Copyright 2023 Google LLC
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

POWERSHELL = powershell -NoProfile -ExecutionPolicy Unrestricted
CREATE_MANIFEST = $(POWERSHELL) $(MAKEDIR)\..\..\sources\scripts\create-winget-manifest.ps1

default: winget-manifests clone-repo

public-folder:
    if not exist $(MAKEDIR)\public \
        md $(MAKEDIR)\public

winget-manifests: public-folder
    if not exist $(MAKEDIR)\public\manifest \
        md $(MAKEDIR)\public\manifest

	$(CREATE_MANIFEST) -ManifestType Version > $(MAKEDIR)\public\manifest\IAPDesktop.yaml
	$(CREATE_MANIFEST) -ManifestType DefaultLocale > $(MAKEDIR)\public\manifest\IAPDesktop.locale.en-US.yaml
	$(CREATE_MANIFEST) -ManifestType Installer > $(MAKEDIR)\public\manifest\IAPDesktop.installer.yaml

	winget validate --manifest $(MAKEDIR)\public\manifest

clone-repo: public-folder
	if not exist $(MAKEDIR)\public\winget-pkgs \
		git clone --depth 1 https://github.com/microsoft/winget-pkgs.git $(MAKEDIR)\public\winget-pkgs

	cd $(MAKEDIR)\public\winget-pkgs
	git fetch
	git checkout origin/master
	cd $(MAKEDIR)