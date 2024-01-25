/*
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
using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Credentials;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Credentials;

[TestClass]
public class GetCredentialsResponseTests
{
    [TestMethod]
    public void SerializeObject_ResponseWithToken_SerializedCorrectly()
    {
        var testSubject = new GetCredentialsResponse(new TokenDto("token123"));

        var serializeObject = JsonConvert.SerializeObject(testSubject);
        serializeObject.Should().Be("""{"credentials":{"token":"token123"}}""");
    }

    [TestMethod]
    public void Ctor_ResponseWithToken_CredentialsSetCorrectly()
    {
        var tokenDto = new TokenDto("token123");
        var testSubject = new GetCredentialsResponse(tokenDto);

        testSubject.credentials.Left.Should().BeSameAs(tokenDto);
        testSubject.credentials.Right.Should().BeNull();
    }
    
    [TestMethod]
    public void Ctor_NullTokenDto_Throws()
    {
        Action act = () => new GetCredentialsResponse(token: null);

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [TestMethod]
    public void SerializeObject_ResponseWithUserAndPassword_SerializedCorrectly()
    {
        var testSubject = new GetCredentialsResponse(new UsernamePasswordDto("user123", "password123"));

        var serializeObject = JsonConvert.SerializeObject(testSubject);
        serializeObject.Should().Be("""{"credentials":{"username":"user123","password":"password123"}}""");
    }
    
    [TestMethod]
    public void Ctor_ResponseWithUserAndPassword_CredentialsSetCorrectly()
    {
        var usernamePasswordDto = new UsernamePasswordDto("user123", "password123");
        var testSubject = new GetCredentialsResponse(usernamePasswordDto);

        testSubject.credentials.Right.Should().BeSameAs(usernamePasswordDto);
        testSubject.credentials.Left.Should().BeNull();
    }
    
    [TestMethod]
    public void Ctor_NullUserAndPasswordDto_Throws()
    {
        Action act = () => new GetCredentialsResponse(usernamePassword: null);

        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [TestMethod]
    public void Ctor_NoCredentials_HasNullCredentialsProperty()
    {
        GetCredentialsResponse.NoCredentials.credentials.Should().BeNull();
    }
    
    [TestMethod]
    public void ResponseWithNoCredentials_SerializedCorrectly()
    {
        JsonConvert.SerializeObject(GetCredentialsResponse.NoCredentials).Should().Be("""{"credentials":null}""");
    }
}
