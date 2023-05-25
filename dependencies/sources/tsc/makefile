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
CONFIGURATION = Release

# The tag should be increased whenever one of the dependencies is changed
TAG = 1

TSC_VERSION = 9.0.0.$(TAG)

default: package

$(MAKEDIR)\bin\$(CONFIGURATION)\Google.Solutions.Tsc.$(TSC_VERSION).nupkg:
	@echo "========================================================"
	@echo "=== Building tsc                                     ==="
	@echo "========================================================"

	msbuild \
		/t:Restore;Build \
		"/p:Configuration=$(CONFIGURATION);TargetFrameworks=net462;TargetFrameworkVersion=v4.6.2;Platform=Any CPU;Version=$(TSC_VERSION);OutputPath=bin\$(CONFIGURATION)" \
		"$(MAKEDIR)\Google.Solutions.Tsc.sln"
        
    	nuget pack -OutputDirectory $(MAKEDIR)\obj\$(CONFIGURATION)\ <<Google.Solutions.Tsc.nuspec
<?xml version="1.0"?>
<package>
  <metadata>
    <id>Google.Solutions.Tsc</id>
    <version>$(TSC_VERSION)</version>
    <authors>Google LLC</authors>
    <owners>Google LLC</owners>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Terminal service client</description><dependencies>
      <group targetFramework=".NETFramework4.6.2" />
    </dependencies>
  </metadata>
  <files>
    <file src="$(MAKEDIR)\bin\$(CONFIGURATION)\AxInterop.MSTSCLib.dll" target="lib\net462\AxInterop.MSTSCLib.dll" />
    <file src="$(MAKEDIR)\bin\$(CONFIGURATION)\Google.Solutions.Tsc.dll" target="lib\net462\Google.Solutions.Tsc.dll" />
    <file src="$(MAKEDIR)\bin\$(CONFIGURATION)\Interop.MSTSCLib.dll" target="lib\net462\Interop.MSTSCLib.dll" />
  </files>
</package>
<<NOKEEP

#------------------------------------------------------------------------------
# Main targets
#------------------------------------------------------------------------------

package: $(MAKEDIR)\bin\$(CONFIGURATION)\Google.Solutions.Tsc.$(TSC_VERSION).nupkg
    copy /Y $(MAKEDIR)\obj\$(CONFIGURATION)\Google.Solutions.Tsc.$(TSC_VERSION).nupkg $(MAKEDIR)\Google.Solutions.Tsc.nupkg

clean:
    msbuild /t:Clean "$(MAKEDIR)\Google.Solutions.Tsc.csproj"
    -rd /S /Q $(MAKEDIR)\bin
    -rd /S /Q $(MAKEDIR)\obj
    -del $(MAKEDIR)\Google.Solutions.Tsc.nupkg
    
distclean: clean