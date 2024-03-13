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

using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common;

[TestClass]
public class UserAndPasswordDtoTests
{
    [TestMethod]
    public void Ctor_SetsPropertiesCorrectly()
    {
        var username = "user123";
        var password = "password123";

        var testSubject = new UsernamePasswordDto(username, password);

        testSubject.username.Should().BeSameAs(username);
        testSubject.password.Should().BeSameAs(password);
    }
    
    [TestMethod]
    public void Ctor_NullParameter_Throws()
    {
        Action actUsr = () => new UsernamePasswordDto(null, "password123");
        Action actPwd = () => new UsernamePasswordDto("user123", null);

        actUsr.Should().ThrowExactly<ArgumentNullException>();
        actPwd.Should().ThrowExactly<ArgumentNullException>();
    }
}
