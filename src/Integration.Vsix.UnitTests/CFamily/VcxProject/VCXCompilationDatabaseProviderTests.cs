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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class VCXCompilationDatabaseProviderTests
{
    private const string SourceFilePath = "some path";
    private IVCXCompilationDatabaseStorage storage;
    private IFileConfigProvider fileConfigProvider;
    private VCXCompilationDatabaseProvider testSubject;

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<VCXCompilationDatabaseProvider, IVCXCompilationDatabaseProvider>(
            MefTestHelpers.CreateExport<IVCXCompilationDatabaseStorage>(),
            MefTestHelpers.CreateExport<IFileConfigProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<VCXCompilationDatabaseProvider>();

    [TestInitialize]
    public void TestInitialize()
    {
        storage = Substitute.For<IVCXCompilationDatabaseStorage>();
        fileConfigProvider = Substitute.For<IFileConfigProvider>();
        testSubject = new VCXCompilationDatabaseProvider(
            storage,
            fileConfigProvider);
    }

    [TestMethod]
    public void CreateOrNull_NoFileConfig_ReturnsNull()
    {
        fileConfigProvider.Get(SourceFilePath, default).ReturnsNull();

        testSubject.CreateOrNull(SourceFilePath).Should().BeNull();

        storage.DidNotReceiveWithAnyArgs().CreateDatabase(default);
    }

    [TestMethod]
    public void CreateOrNull_FileConfig_CantStore_ReturnsNull()
    {
        var fileConfig = Substitute.For<IFileConfig>();
        fileConfigProvider.Get(SourceFilePath, default).Returns(fileConfig);
        storage.CreateDatabase(fileConfig).ReturnsNull();

        testSubject.CreateOrNull(SourceFilePath).Should().BeNull();

        storage.Received().CreateDatabase(fileConfig);
    }

    [TestMethod]
    public void CreateOrNull_FileConfig_StoresAndReturnsPath()
    {
        const string databasePath = "database path";
        var fileConfig = Substitute.For<IFileConfig>();
        fileConfigProvider.Get(SourceFilePath, default).Returns(fileConfig);
        storage.CreateDatabase(fileConfig).Returns(databasePath);

        testSubject.CreateOrNull(SourceFilePath).Should().Be(databasePath);
    }
}
