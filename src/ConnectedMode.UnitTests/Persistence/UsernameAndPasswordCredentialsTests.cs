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

using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class UsernameAndPasswordCredentialsTests
{
    private const string Username = "username";
    private const string Password = "pwd";

    [TestMethod]
    public void Ctor_WhenUsernameIsNull_ThrowsArgumentNullException()
    {
        Action act = () => new UsernameAndPasswordCredentials(null, Password.ToSecureString());

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Ctor_WhenPasswordIsNull_ThrowsArgumentNullException()
    {
        Action act = () => new UsernameAndPasswordCredentials(Username, null);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Dispose_DisposesPassword()
    {
        var testSubject = new UsernameAndPasswordCredentials(Username, Password.ToSecureString());

        testSubject.Dispose();

        Exceptions.Expect<ObjectDisposedException>(() => testSubject.Password.ToUnsecureString());
    }

    [TestMethod]
    public void Clone_ClonesPassword()
    {
        var password = "pwd";
        var testSubject = new UsernameAndPasswordCredentials(Username, password.ToSecureString());

        var clone = (UsernameAndPasswordCredentials)testSubject.Clone();

        clone.Should().NotBeSameAs(testSubject);
        clone.Password.Should().NotBeSameAs(testSubject.Password);
        clone.Password.ToUnsecureString().Should().Be(testSubject.Password.ToUnsecureString());
        clone.UserName.Should().Be(testSubject.UserName);
    }
}
