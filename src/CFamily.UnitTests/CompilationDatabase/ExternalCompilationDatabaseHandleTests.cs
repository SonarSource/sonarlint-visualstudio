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

using SonarLint.VisualStudio.CFamily.CompilationDatabase;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CompilationDatabase;

[TestClass]
public class ExternalCompilationDatabaseHandleTests
{
    [TestMethod]
    public void Ctor_AssignsExpectedValues()
    {
        const string filePath = "some path";
        var testSubject = new ExternalCompilationDatabaseHandle(filePath);

        testSubject.FilePath.Should().BeSameAs(filePath);
    }

    [TestMethod]
    public void Ctor_NullPath_Throws()
    {
        var act = () => new ExternalCompilationDatabaseHandle(null);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("filePath");
    }

    [TestMethod]
    public void Dispose_NoOp_DoesNotThrow()
    {
        var act = () => new ExternalCompilationDatabaseHandle("some path").Dispose();

        act.Should().NotThrow();
    }
}
