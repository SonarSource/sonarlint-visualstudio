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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class LiveAnalysisStateFactoryTests
{
    private ITaskExecutorWithDebounceFactory taskExecutorWithDebounceFactory;
    private ILinkedFileAnalyzer linkedFileAnalyzer;
    private IFileTracker fileTracker;
    private LiveAnalysisStateFactory testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        taskExecutorWithDebounceFactory = Substitute.For<ITaskExecutorWithDebounceFactory>();
        fileTracker = Substitute.For<IFileTracker>();
        linkedFileAnalyzer = Substitute.For<ILinkedFileAnalyzer>();
        testSubject = new LiveAnalysisStateFactory(taskExecutorWithDebounceFactory, fileTracker, linkedFileAnalyzer);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LiveAnalysisStateFactory, ILiveAnalysisStateFactory>(
            MefTestHelpers.CreateExport<ITaskExecutorWithDebounceFactory>(),
            MefTestHelpers.CreateExport<IFileTracker>(),
            MefTestHelpers.CreateExport<ILinkedFileAnalyzer>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<LiveAnalysisStateFactory>();

    [TestMethod]
    public void Create_ReturnsExpectedValue()
    {
        var fileState = Substitute.For<IFileState>();

        var liveAnalysisState = testSubject.Create(fileState);

        liveAnalysisState.FileState.Should().Be(fileState);
        taskExecutorWithDebounceFactory.Received(1).Create();
    }
}
