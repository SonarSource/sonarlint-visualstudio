/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.LocalServices;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices;

[TestClass]
public class CanonicalFilePathProviderTests
{
    private const string FilePath = @"C:\file.txt";
    private const string CanonicalPath = @"C:\File.Txt";

    private ICanonicalFilePathsCache cache;
    private IVsUIServiceOperation uiServiceOperation;
    private IVsRunningDocumentTable runningDocumentTable;
    private DTE2 dte2;
    private IVsProject project;
    private readonly uint defaultItemId = 123;
    private CanonicalFilePathProvider testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        runningDocumentTable = Substitute.For<IVsRunningDocumentTable>();
        runningDocumentTable.FindAndLockDocument(Arg.Any<uint>(), Arg.Any<string>(), out Arg.Any<IVsHierarchy>(), out Arg.Any<uint>(), out Arg.Any<IntPtr>(), out Arg.Any<uint>()).ReturnsForAnyArgs(VSConstants.S_FALSE);
        project = Substitute.For<IVsProject, IVsHierarchy>();
        dte2 = Substitute.For<DTE2>();
        dte2.Solution.FindProjectItem(default).ReturnsNullForAnyArgs();
        cache = Substitute.For<ICanonicalFilePathsCache>();
        uiServiceOperation = Substitute.For<IVsUIServiceOperation>();
        testSubject = new CanonicalFilePathProvider(cache, uiServiceOperation);

        uiServiceOperation.Execute<SVsRunningDocumentTable, IVsRunningDocumentTable, string>(Arg.Any<Func<IVsRunningDocumentTable, string>>())
            .Returns(info => info.Arg<Func<IVsRunningDocumentTable, string>>()(runningDocumentTable));


        uiServiceOperation.Execute<SDTE, DTE2, string>(Arg.Any<Func<DTE2, string>>()).Returns(info => info.Arg<Func<DTE2, string>>()(dte2));
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<CanonicalFilePathProvider, ICanonicalFilePathProvider>(
            MefTestHelpers.CreateExport<ICanonicalFilePathsCache>(),
            MefTestHelpers.CreateExport<IVsUIServiceOperation>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<CanonicalFilePathProvider>();

    [TestMethod]
    public void GetCanonicalPath_OriginalFilePathIsNull_ReturnsNull()
    {
        var result = testSubject.GetCanonicalPath(null);
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetCanonicalPath_OriginalFilePathIsEmpty_ReturnsNull()
    {
        var result = testSubject.GetCanonicalPath("");
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetCanonicalPath_OriginalFilePathIsWhitespace_ReturnsNull()
    {
        var result = testSubject.GetCanonicalPath("   ");
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetCanonicalPath_CacheHit_ReturnsCachedPath()
    {
        SetUpCache(FilePath, CanonicalPath);

        var result = testSubject.GetCanonicalPath(FilePath);

        result.Should().Be(CanonicalPath);
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtHit_ReturnsRdtPathAndAddsToCache()
    {

        SetUpProject(defaultItemId, CanonicalPath);
        SetupUiServiceOperationForRdt(FilePath, project as IVsHierarchy, defaultItemId);

        var result = testSubject.GetCanonicalPath(FilePath);

        result.Should().Be(CanonicalPath);
        cache.Received(1).Add(CanonicalPath);
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtHasNoProject_ReturnsOriginalFilePath()
    {
        SetUpProject(defaultItemId, CanonicalPath);
        SetupUiServiceOperationForRdt(FilePath, Substitute.For<IVsHierarchy>(), defaultItemId);

        var result = testSubject.GetCanonicalPath(FilePath);

        result.Should().Be(FilePath);
        cache.DidNotReceiveWithAnyArgs().Add(default(string));
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtHasInvalidItemIdProject_ReturnsOriginalFilePath()
    {
        SetUpProject(defaultItemId, CanonicalPath);
        SetupUiServiceOperationForRdt(FilePath, project as IVsHierarchy, (uint)VSConstants.VSITEMID.Nil);

        var result = testSubject.GetCanonicalPath(FilePath);

        result.Should().Be(FilePath);
        cache.DidNotReceiveWithAnyArgs().Add(default(string));
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtProjectHasNoFilePath_ReturnsOriginalFilePath()
    {
        var filePath = "file.txt";
        SetUpProject(defaultItemId, null);
        SetupUiServiceOperationForRdt(filePath, project as IVsHierarchy, defaultItemId);

        var result = testSubject.GetCanonicalPath(filePath);

        result.Should().Be(filePath);
        cache.DidNotReceiveWithAnyArgs().Add(default(string));
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtHasDifferentFilePath_ReturnsOriginalFilePath()
    {
        var filePath = "file.txt";
        SetUpProject(defaultItemId, "Some other path");
        SetupUiServiceOperationForRdt(filePath, project as IVsHierarchy, defaultItemId);

        var result = testSubject.GetCanonicalPath(filePath);

        result.Should().Be(filePath);
        cache.DidNotReceiveWithAnyArgs().Add(default(string));
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtMiss_SolutionHit_ReturnsSolutionPathAndAddsToCache()
    {
        SetupUiServiceOperationForSolution(FilePath, CanonicalPath);

        var result = testSubject.GetCanonicalPath(FilePath);

        result.Should().Be(CanonicalPath);
        cache.Received(1).Add(CanonicalPath);
    }

    [TestMethod]
    public void GetCanonicalPath_CacheMiss_RdtMiss_SolutionMiss_ReturnsOriginalFilePath()
    {
        var result = testSubject.GetCanonicalPath(FilePath);

        result.Should().Be(FilePath);
        cache.DidNotReceiveWithAnyArgs().Add(default(string));
    }

    private void SetUpCache(string filePath, string canonicalPath)
    {
        cache.TryGet(filePath, out Arg.Any<string>()).Returns(x => { x[1] = canonicalPath; return true; });
    }

    private void SetupUiServiceOperationForRdt(string filePath, IVsHierarchy projectHierarchy, uint itemId)
    {
        runningDocumentTable.FindAndLockDocument(
                Arg.Is((uint)_VSRDTFLAGS.RDT_NoLock), // only get the hierarchy info, no file locking
                Arg.Is(filePath),
                out Arg.Any<IVsHierarchy>(),
                out Arg.Any<uint>(),
                out Arg.Any<IntPtr>(),
                out Arg.Any<uint>())
            .Returns(info =>
            {
                info[2] = projectHierarchy;
                info[3] = itemId;
                return VSConstants.S_OK;
            });


    }

    private void SetUpProject(uint itemId, string filePath) =>
        project.GetMkDocument(itemId, out Arg.Any<string>()).Returns(info =>
        {
            info[1] = filePath;
            return VSConstants.S_OK;
        });

    private void SetupUiServiceOperationForSolution(string filePath, string canonicalPath)
    {
        var projectItem = Substitute.For<ProjectItem>();
        dte2.Solution.FindProjectItem(filePath).Returns(projectItem);
        projectItem.FileNames[0].Returns(canonicalPath);
    }
}
