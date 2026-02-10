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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.LocalServices;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices;

[TestClass]
public class CanonicalFilePathsCacheTests
{
    private IActiveSolutionTracker activeSolutionTracker;
    private CanonicalFilePathsCache cache;

    [TestInitialize]
    public void TestInitialize()
    {
        activeSolutionTracker = Substitute.For<IActiveSolutionTracker>();
        cache = new CanonicalFilePathsCache(activeSolutionTracker);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<CanonicalFilePathsCache, ICanonicalFilePathsCache>(
            MefTestHelpers.CreateExport<IActiveSolutionTracker>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<CanonicalFilePathsCache>();

    [TestMethod]
    [DataRow("C:/file1.txt")]
    [DataRow("C:/FiLe1.tXt")]
    [DataRow("C:/FILE1.TXT")]
    public void Add_SingleFilePath_ShouldBeRetrievable(string filePath)
    {
        const string canonicalFilePath = "C:/file1.txt";

        cache.Add(canonicalFilePath);

        ValidateFilePathCached(filePath, canonicalFilePath);
    }

    [TestMethod]
    [DataRow("C:/a.txt", "C:/a.txt")]
    [DataRow("C:/A.tXt", "C:/a.txt")]
    [DataRow("C:/A.TXT", "C:/A.txt")]
    public void Add_MultipleFilePaths_ShouldBeRetrievable(string filePath, string expectedCanonicalPath)
    {
        var canonicalPaths = new[] { "C:/any.txt", expectedCanonicalPath, "C:/other.txt" };

        cache.Add(canonicalPaths);

        foreach (var path in canonicalPaths)
        {
            ValidateFilePathCached(path, path);
        }
        ValidateFilePathCached(filePath, expectedCanonicalPath);
    }

    [TestMethod]
    public void TryGet_FileNotAdded_ShouldReturnFalse()
    {
        var result = cache.TryGet("C:/notfound.txt", out var canonicalPath);

        result.Should().BeFalse();

        canonicalPath.Should().BeNull();
    }

    [TestMethod]
    public void Add_DuplicateFilePath_ShouldOverwrite()
    {
        var filePath = "C:/file.txt";
        cache.Add(filePath);
        var filePath2 = "C:/File.txt";
        cache.Add(filePath2);

        cache.TryGet(filePath, out var canonicalPath).Should().BeTrue();

        canonicalPath.Should().Be(filePath2);
    }

    [TestMethod]
    public void ActiveSolutionChanged_SolutionClosed_ShouldClearCache()
    {
        var filePath = "C:/file.txt";
        cache.Add(filePath);

        RaiseSolutionChanged();

        cache.TryGet(filePath, out _).Should().BeFalse();
    }

    [TestMethod]
    public void ActiveSolutionChanged_SolutionOpen_ShouldNotClearCache()
    {
        var filePath = "C:/file.txt";
        cache.Add(filePath);

        RaiseSolutionChanged(isOpen: true);

        cache.TryGet(filePath, out var canonicalPath).Should().BeTrue();
        canonicalPath.Should().Be(filePath);
    }

    [TestMethod]
    public void Dispose_ShouldUnsubscribeAndClearCache()
    {
        var filePath = "C:/file.txt";
        cache.Add(filePath);

        cache.Dispose();

        cache.Invoking(c => c.Add(filePath)).Should().Throw<ObjectDisposedException>();
        activeSolutionTracker.Received(1).ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
    }

    [TestMethod]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        cache.Dispose();
        cache.Invoking(c => c.Dispose()).Should().NotThrow();
    }

    [TestMethod]
    public void TryGet_AfterDispose_ShouldThrow()
    {
        cache.Dispose();
        cache.Invoking(c => c.TryGet("C:/file.txt", out _)).Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void Add_AfterDispose_ShouldThrow()
    {
        cache.Dispose();
        cache.Invoking(c => c.Add("C:/file.txt")).Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void Add_MultipleFiles_AfterDispose_ShouldThrow()
    {
        cache.Dispose();
        cache.Invoking(c => c.Add(["C:/a.txt", "C:/b.txt"])).Should().Throw<ObjectDisposedException>();
    }

    private void ValidateFilePathCached(string filePath, string expectedCanonicalPath)
    {
        var result = cache.TryGet(filePath, out var canonicalPath);
        result.Should().BeTrue();
        canonicalPath.Should().Be(expectedCanonicalPath);
    }

    private void RaiseSolutionChanged(bool isOpen = false)
    {
        var args = new ActiveSolutionChangedEventArgs(isOpen, "TestSolution");
        activeSolutionTracker.ActiveSolutionChanged += Raise.Event<EventHandler<ActiveSolutionChangedEventArgs>>(activeSolutionTracker, args);
    }
}
