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

using Microsoft.CodeAnalysis.CodeActions;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Integration.Vsix.RoslynQuickFixes;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.RoslynAnalyzerServer;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;

namespace SonarLint.VisualStudio.Integration.UnitTests.RoslynQuickFixes;

[TestClass]
public class RoslynQuickFixStorageTests
{
    private IActiveConfigScopeTracker configScopeTracker;
    private RoslynQuickFixStorage testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        configScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        testSubject = new RoslynQuickFixStorage(configScopeTracker);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        var requiredExports = new []{MefTestHelpers.CreateExport<IActiveConfigScopeTracker>()};
        MefTestHelpers.CheckTypeCanBeImported<RoslynQuickFixStorage, IRoslynQuickFixStorageWriter>(requiredExports);
        MefTestHelpers.CheckTypeCanBeImported<RoslynQuickFixStorage, IRoslynQuickFixProvider>(requiredExports);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<RoslynQuickFixStorage>();

    [TestMethod]
    public void Add_ThenTryGet_ReturnsTrueAndQuickFix()
    {
        var quickFixImpl = CreateApplication();

        testSubject.Add(quickFixImpl);

        var result = testSubject.TryGet(quickFixImpl.Id, out var retrievedQuickFix);

        result.Should().BeTrue();
        retrievedQuickFix.Should().BeOfType<RoslynQuickFixApplication>().Which.Implementation.Should().BeSameAs(quickFixImpl);
    }

    [TestMethod]
    public void TryGet_NonExistentId_ReturnsFalseAndNull()
    {
        var result = testSubject.TryGet(Guid.NewGuid(), out var retrievedQuickFix);

        result.Should().BeFalse();
        retrievedQuickFix.Should().BeNull();
    }

    [TestMethod]
    public void ConfigScopeTracker_OnCurrentConfigurationScopeChanged_DefinitionChanged_ClearsCache()
    {
        var quickFixImpl = CreateApplication();
        testSubject.Add(quickFixImpl);

        configScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(
            new ConfigurationScopeChangedEventArgs(definitionChanged: true));

        var result = testSubject.TryGet(quickFixImpl.Id, out var retrievedQuickFix);

        result.Should().BeFalse();
        retrievedQuickFix.Should().BeNull();
    }

    [TestMethod]
    public void ConfigScopeTracker_OnCurrentConfigurationScopeChanged_DefinitionNotChanged_DoesNotClearCache()
    {
        var quickFixImpl = CreateApplication();
        testSubject.Add(quickFixImpl);

        configScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(
            new ConfigurationScopeChangedEventArgs(definitionChanged: false));

        var result = testSubject.TryGet(quickFixImpl.Id, out var retrievedQuickFix);

        result.Should().BeTrue();
        retrievedQuickFix.Should().BeOfType<RoslynQuickFixApplication>().Which.Implementation.Should().BeSameAs(quickFixImpl);
    }

    [TestMethod]
    public void Clear_RemovesForFile()
    {
        const string file1 = "1.cs";
        const string file2 = "2.cs";
        var quickFixImpl1 = CreateApplication(file1);
        var quickFixImpl2 = CreateApplication(file2);
        testSubject.Add(quickFixImpl1);
        testSubject.Add(quickFixImpl2);

        testSubject.Clear(file1);

        testSubject.TryGet(quickFixImpl1.Id, out _).Should().BeFalse();
        testSubject.TryGet(quickFixImpl2.Id, out var retrievedQuickFix).Should().BeTrue();
        retrievedQuickFix.Should().BeOfType<RoslynQuickFixApplication>().Which.Implementation.Should().BeSameAs(quickFixImpl2);
    }

    private static RoslynQuickFixApplicationImpl CreateApplication(string filePath = "any") =>
        new(Substitute.For<IRoslynWorkspaceWrapper>(), Substitute.For<IRoslynSolutionWrapper>(), Substitute.For<IRoslynCodeActionWrapper>(), filePath);
}
