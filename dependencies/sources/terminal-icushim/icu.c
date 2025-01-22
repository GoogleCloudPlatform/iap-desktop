//
// Copyright 2024 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

#include <icu.h>
#include <windows.h>

#pragma warning(disable: 28301)

static void SetNotSupported(
    _Out_ UErrorCode* status) 
{
    if (status)
    {
        *status = U_UNSUPPORTED_ERROR;
    }
}

U_CAPI URegularExpression* U_EXPORT2 uregex_open(
    _In_ const  UChar* pattern,
    _In_ int32_t patternLength,
    _In_ uint32_t flags,
    _Inout_ UParseError* pe,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(pattern);
    UNREFERENCED_PARAMETER(patternLength);
    UNREFERENCED_PARAMETER(flags);
    UNREFERENCED_PARAMETER(pe);

    SetNotSupported(status);
    return NULL;
}

U_CAPI void U_EXPORT2 uregex_close(
    _In_ URegularExpression* regexp)
{
    UNREFERENCED_PARAMETER(regexp);
}

U_CAPI UText* U_EXPORT2 utext_setup(
    _In_ UText* ut,
    _In_ int32_t extraSpace, 
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(ut);
    UNREFERENCED_PARAMETER(extraSpace);

    SetNotSupported(status);
    return NULL;
}

U_CAPI UText* U_EXPORT2 utext_close(
    _In_ UText* ut)
{
    UNREFERENCED_PARAMETER(ut);
    return NULL;
}

U_CAPI int64_t U_EXPORT2 uregex_start64(
    _In_ URegularExpression* regexp,
    _In_ int32_t groupNum,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);
    UNREFERENCED_PARAMETER(groupNum);

    SetNotSupported(status);
    return 0;
}

U_CAPI void U_EXPORT2 uregex_setTimeLimit(
    _In_ URegularExpression* regexp,
    _In_ int32_t limit,
    _Out_ UErrorCode* status) 
{
    UNREFERENCED_PARAMETER(regexp);
    UNREFERENCED_PARAMETER(limit);

    SetNotSupported(status);
}

U_CAPI void U_EXPORT2 uregex_setStackLimit(
    _In_ URegularExpression* regexp,
    _In_ int32_t limit,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);
    UNREFERENCED_PARAMETER(limit);

    SetNotSupported(status);
}

U_CAPI UBool U_EXPORT2 uregex_findNext(
    _In_ URegularExpression* regexp,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);

    SetNotSupported(status);
    return 0;
}

U_CAPI UBool U_EXPORT2 uregex_find(
    _In_ URegularExpression* regexp,
    _In_ int32_t startIndex,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);
    UNREFERENCED_PARAMETER(startIndex);

    SetNotSupported(status);
    return 0;
}

U_CAPI void U_EXPORT2 uregex_setUText(
    _In_ URegularExpression* regexp,
    _In_ UText* text,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);
    UNREFERENCED_PARAMETER(text);

    SetNotSupported(status);
}

U_CAPI int64_t U_EXPORT2 uregex_end64(
    _In_ URegularExpression* regexp,
    _In_ int32_t groupNum,
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);
    UNREFERENCED_PARAMETER(groupNum);

    SetNotSupported(status);
    return 0;
}

U_CAPI URegularExpression* U_EXPORT2 uregex_clone(
    _In_ const URegularExpression* regexp, 
    _Out_ UErrorCode* status)
{
    UNREFERENCED_PARAMETER(regexp);

    SetNotSupported(status);
    return 0;
}