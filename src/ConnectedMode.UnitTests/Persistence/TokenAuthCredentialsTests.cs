/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class TokenAuthCredentialsTests
{
    private const string Token = "token";

    [TestMethod]
    public void Ctor_WhenTokenIsNull_ThrowsArgumentNullException()
    {
        Action act = () => new TokenAuthCredentials(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Dispose_DisposesPassword()
    {
        var testSubject = new TokenAuthCredentials(Token.ToSecureString());

        testSubject.Dispose();

        Exceptions.Expect<ObjectDisposedException>(() => testSubject.Token.ToUnsecureString());
    }

    [TestMethod]
    public void Clone_ClonesPassword()
    {
        var testSubject = new TokenAuthCredentials(Token.ToSecureString());

        var clone = (TokenAuthCredentials)testSubject.Clone();

        clone.Should().NotBeSameAs(testSubject);
        clone.Token.Should().NotBeSameAs(testSubject.Token);
        clone.Token.ToUnsecureString().Should().Be(testSubject.Token.ToUnsecureString());
    }
}
