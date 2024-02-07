﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Credentials;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Credentials;

[TestClass]
public class CredentialsListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<CredentialsListener, ISLCoreListener>();
    }
    
    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<CredentialsListener>();
    }

    [TestMethod]
    public async Task GetCredentialsAsync_StubImpl_ReturnsNoCredentials()
    {
        var testSubject = CreateTestSubject();

        var response = await testSubject.GetCredentialsAsync(new GetCredentialsParams($"randomstring-{Guid.NewGuid()}"));

        response.Should().BeSameAs(GetCredentialsResponse.NoCredentials);
    }

    private CredentialsListener CreateTestSubject()
    {
        return new CredentialsListener();
    }
}