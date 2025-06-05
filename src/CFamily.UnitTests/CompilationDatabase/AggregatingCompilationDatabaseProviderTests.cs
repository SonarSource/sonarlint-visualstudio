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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CompilationDatabase;

[TestClass]
public class AggregatingCompilationDatabaseProviderTests
{
    private ICMakeCompilationDatabaseLocator cmake;
    private IObsoleteVcxCompilationDatabaseProvider vcx;
    private AggregatingCompilationDatabaseProvider testSubject;

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AggregatingCompilationDatabaseProvider, IAggregatingCompilationDatabaseProvider>(
            MefTestHelpers.CreateExport<ICMakeCompilationDatabaseLocator>(),
            MefTestHelpers.CreateExport<IObsoleteVcxCompilationDatabaseProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AggregatingCompilationDatabaseProvider>();

    [TestInitialize]
    public void TestInitialize()
    {
        cmake = Substitute.For<ICMakeCompilationDatabaseLocator>();
        vcx = Substitute.For<IObsoleteVcxCompilationDatabaseProvider>();
        testSubject = new AggregatingCompilationDatabaseProvider(cmake, vcx);
    }

    [TestMethod]
    public void GetOrNull_CmakeAvailable_ReturnsCmakeLocation()
    {
        var location = "some location";
        cmake.Locate().Returns(location);

        var result = testSubject.GetOrNull("some path");

        result.Should().Be(location);
        vcx.DidNotReceiveWithAnyArgs().CreateOrNull(default);
    }

    [TestMethod]
    public void GetOrNull_CmakeUnavailable_VcxAvailable_ReturnsVcxLocation()
    {
        var sourcePath = "some path";
        var location = "some location";
        cmake.Locate().ReturnsNull();
        vcx.CreateOrNull(sourcePath).Returns(location);

        var result = testSubject.GetOrNull(sourcePath);

        result.Should().Be(location);
        cmake.Received().Locate();
    }

    [TestMethod]
    public void GetOrNull_Unavailable_ReturnsNull()
    {
        cmake.Locate().ReturnsNull();
        vcx.CreateOrNull(default).ReturnsNullForAnyArgs();

        var result = testSubject.GetOrNull("some path");

        result.Should().BeNull();
    }
}
