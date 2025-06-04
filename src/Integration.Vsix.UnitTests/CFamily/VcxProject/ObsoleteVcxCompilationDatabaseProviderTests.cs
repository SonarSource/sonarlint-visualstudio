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
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class ObsoleteVcxCompilationDatabaseProviderTests
{
    private const string SourceFilePath = "some path";
    private IActiveVcxCompilationDatabase activeDatabase;
    private ICompilationDatabaseEntryGenerator generator;
    private ObsoleteVcxCompilationDatabaseProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        activeDatabase = Substitute.For<IActiveVcxCompilationDatabase>();
        generator = Substitute.For<ICompilationDatabaseEntryGenerator>();
        testSubject = new ObsoleteVcxCompilationDatabaseProvider(activeDatabase, generator);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ObsoleteVcxCompilationDatabaseProvider, IObsoleteVcxCompilationDatabaseProvider>(
            MefTestHelpers.CreateExport<IActiveVcxCompilationDatabase>(),
            MefTestHelpers.CreateExport<ICompilationDatabaseEntryGenerator>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ObsoleteVcxCompilationDatabaseProvider>();

    [TestMethod]
    public void CreateOrNull_GeneratorReturnsNull_ReturnsNull()
    {
        generator.CreateOrNull(SourceFilePath).ReturnsNull();

        var result = testSubject.CreateOrNull(SourceFilePath);

        result.Should().BeNull();
    }

    [TestMethod]
    public void CreateOrNull_ActiveDatabasePathNull_ReturnsNull()
    {
        generator.CreateOrNull(SourceFilePath).Returns(new CompilationDatabaseEntry());
        activeDatabase.DatabasePath.ReturnsNull();

        var result = testSubject.CreateOrNull(SourceFilePath);

        result.Should().BeNull();
    }

    [TestMethod]
    public void CreateOrNull_GeneratorAndDatabasePathPresent_ReturnsHandle()
    {
        generator.CreateOrNull(SourceFilePath).Returns(new CompilationDatabaseEntry());
        const string dbPath = "some-db-path.json";
        activeDatabase.DatabasePath.Returns(dbPath);

        var result = testSubject.CreateOrNull(SourceFilePath);

        result.Should().BeEquivalentTo(new ExternalCompilationDatabaseHandle(dbPath));
    }
}
