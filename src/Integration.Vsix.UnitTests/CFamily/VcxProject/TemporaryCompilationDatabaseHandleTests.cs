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

using System.IO.Abstractions;
using NSubstitute.ReceivedExtensions;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class TemporaryCompilationDatabaseHandleTests
{
    private const string FilePath = "some path";
    private IFile file;
    private TestLogger logger;
    private TemporaryCompilationDatabaseHandle testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        file = Substitute.For<IFile>();
        logger = new TestLogger();
        testSubject = new TemporaryCompilationDatabaseHandle(FilePath, file, logger);
    }

    [DataRow("path1")]
    [DataRow(@"path1\path2")]
    [DataTestMethod]
    public void Ctor_AssignsExpectedValues(string path) =>
        new TemporaryCompilationDatabaseHandle(path, default, default).FilePath.Should().BeSameAs(path);

    [TestMethod]
    public void Ctor_NullPath_Throws()
    {
        var act = () => new TemporaryCompilationDatabaseHandle(null, default, default);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("filePath");
    }

    [TestMethod]
    public void Dispose_DeletesFile()
    {
        testSubject.Dispose();

        file.Received().Delete(FilePath);
        logger.AssertNoOutputMessages();
    }

    [TestMethod]
    public void Dispose_MultipleTimes_ActsOnlyOnce()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        file.ReceivedWithAnyArgs(1).Delete(default);
    }

    [TestMethod]
    public void Dispose_CatchesAndLogsExceptions()
    {
        var exception = new Exception("testexc");
        file.When(x => x.Delete(Arg.Any<string>())).Throw(exception);

        var act = () => testSubject.Dispose();

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists(exception.ToString());
    }
}
