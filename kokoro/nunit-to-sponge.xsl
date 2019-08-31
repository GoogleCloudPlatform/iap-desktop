<?xml version="1.0" encoding="utf-8"?>
<!--
    Copyright 2019 Google LLC
    
    Licensed to the Apache Software Foundation (ASF) under one
    or more contributor license agreements.  See the NOTICE file
    distributed with this work for additional information
    regarding copyright ownership.  The ASF licenses this file
    to you under the Apache License, Version 2.0 (the
    "License"); you may not use this file except in compliance
    with the License.  You may obtain a copy of the License at
    
    http://www.apache.org/licenses/LICENSE-2.0
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the License is distributed on an
    "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
    KIND, either express or implied.  See the License for the
    specific language governing permissions and limitations
    under the License.
-->
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/test-run">
    <testsuites tests="{@testcasecount}" failures="{@failed}" disabled="{@skipped}" time="{@duration}">
      <xsl:apply-templates/>
    </testsuites>
  </xsl:template>

  <xsl:template match="test-suite[test-case]">
    <testsuite
          name="{@fullname}"
          tests="{@testcasecount}"
          errors="{@testcasecount - @passed - @skipped - @failed}"
          failures="{@failed}"
          skipped="{@skipped}"
          time="{@duration}">

      <xsl:apply-templates select="test-case"/>
    </testsuite>
    <xsl:apply-templates select="test-suite"/>
  </xsl:template>

  <xsl:template match="test-case">
    <xsl:choose>
      <xsl:when test="@runstate = 'Skipped' or @runstate = 'Ignored'">
        <testcase
              name="{@name}"
              status="notrun"
              classname="{@classname}">
          <xsl:apply-templates/>
        </testcase>
      </xsl:when>
      <xsl:otherwise>
        <testcase
              name="{@name}"
              time="{@duration}"
              status="run"
              classname="{@classname}">
          <xsl:apply-templates/>
        </testcase>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="test-case/failure">
    <failure message="{./message}">
      <xsl:value-of select="./stack-trace"/>
    </failure>
  </xsl:template>

  <xsl:template match="test-case/reason">
    <skipped message="{./message}"/>
  </xsl:template>

  <xsl:template match="output"/>
  <xsl:template match="filter"/>
  <xsl:template match="command-line"/>
  <xsl:template match="settings"/>
  <xsl:template match="test-case/assertions"/>
  <xsl:template match="test-suite/reason"/>
  <xsl:template match="test-suite/failure"/>
  <xsl:template match="properties"/>
  <xsl:template match="stack-trace"/>
</xsl:stylesheet>
