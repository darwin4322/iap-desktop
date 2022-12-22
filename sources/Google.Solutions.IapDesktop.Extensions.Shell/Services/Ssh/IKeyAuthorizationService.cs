﻿//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Locator;
using Google.Solutions.Ssh.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Services.Ssh
{
    public interface IKeyAuthorizationService
    {
        Task<AuthorizedKeyPair> AuthorizeKeyAsync(
            InstanceLocator instance,
            ISshKeyPair key,
            TimeSpan keyValidity,
            string preferredPosixUsername,
            KeyAuthorizationMethods methods,
            CancellationToken token);
    }

    [Flags]
    public enum KeyAuthorizationMethods
    {
        InstanceMetadata = 1,
        ProjectMetadata = 2,
        Oslogin = 4,
        All = 7
    }

}
