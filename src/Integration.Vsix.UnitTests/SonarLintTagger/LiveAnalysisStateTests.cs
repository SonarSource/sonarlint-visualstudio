/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class LiveAnalysisStateTests
{
    private ITaskExecutorWithDebounce executor;
    private IFileState fileState;
    private IFileTracker fileTracker;
    private LiveAnalysisState testSubject;

    private const string FilePath = "file.cs";
    private const string Content = "content";

    [TestInitialize]
    public void TestInitialize()
    {
        executor = Substitute.For<ITaskExecutorWithDebounce>();
        fileState = Substitute.For<IFileState>();
        fileTracker = Substitute.For<IFileTracker>();
        testSubject = new LiveAnalysisState(executor, fileState, fileTracker);
    }

    [TestMethod]
    public void FileState_ShouldReturnInjectedFileState()
    {
        var result = testSubject.FileState;

        result.Should().BeSameAs(fileState);
    }

    [TestMethod]
    public void IsWaiting_ShouldReturnTrue_WhenNotDisposedAndNotScheduled()
    {
        executor.IsScheduled.Returns(false);

        var result = testSubject.IsWaiting;

        result.Should().BeTrue();
    }

    [TestMethod]
    public void IsWaiting_ShouldReturnFalse_WhenDisposed()
    {
        testSubject.Dispose();

        var result = testSubject.IsWaiting;

        result.Should().BeFalse();
    }

    [TestMethod]
    public void IsWaiting_ShouldReturnFalse_WhenScheduled()
    {
        executor.IsScheduled.Returns(true);

        var result = testSubject.IsWaiting;

        result.Should().BeFalse();
    }

    [TestMethod]
    public void HandleLiveAnalysisEvent_ShouldDebounceAnalyzeFile_AndRaiseLinkedAnalysisRequired_WhenTriggered()
    {
        SetUpExecuteImmediately();
        var fileSnapshot = CreateFileSnapshot();
        fileState.UpdateFileState().Returns(fileSnapshot);
        LinkedAnalysisRequiredEventArgs capturedArgs = null;
        testSubject.LinkedAnalysisRequested += (_, e) => capturedArgs = e;

        testSubject.HandleLiveAnalysisEvent(true);

        Received.InOrder(() =>
        {
            executor.Debounce(Arg.Any<Action<CancellationToken>>(), LiveAnalysisState.LiveAnalysisDebounceDuration);
            fileState.UpdateFileState();
            fileTracker.AddFiles(Arg.Is<SourceFile[]>(s => s.Single().FilePath == FilePath && s.Single().Content == Content));
            executor.Debounce(Arg.Any<Action<CancellationToken>>(), LiveAnalysisState.LinkCalculationDebounceDuration);
        });
        capturedArgs.Should().NotBeNull();
        capturedArgs.File.Should().BeSameAs(fileState);
        capturedArgs.Token.Should().Be(CancellationToken.None);
    }

    private void SetUpExecuteImmediately() =>
        executor
            .When(x => x.Debounce(Arg.Any<Action<CancellationToken>>(), Arg.Any<TimeSpan>()))
            .Do(call =>
            {
                var action = call.Arg<Action<CancellationToken>>();
                action(CancellationToken.None);
            });

    [TestMethod]
    public void HandleLiveAnalysisEvent_ShouldNotRaiseLinkedAnalysisRequired_WhenNotTriggered()
    {
        SetUpExecuteImmediately();
        var fileSnapshot = CreateFileSnapshot();
        fileState.UpdateFileState().Returns(fileSnapshot);
        LinkedAnalysisRequiredEventArgs capturedArgs = null;
        testSubject.LinkedAnalysisRequested += (_, e) => capturedArgs = e;

        testSubject.HandleLiveAnalysisEvent(false);

        Received.InOrder(() =>
        {
            executor.Debounce(Arg.Any<Action<CancellationToken>>(), LiveAnalysisState.LiveAnalysisDebounceDuration);
            fileState.UpdateFileState();
            fileTracker.AddFiles(Arg.Is<SourceFile[]>(s => s.Single().FilePath == FilePath && s.Single().Content == Content));
        });
        capturedArgs.Should().BeNull();
    }

    [TestMethod]
    public void HandleLiveAnalysisEvent_ShouldNotAct_WhenDisposed()
    {
        testSubject.Dispose();

        testSubject.HandleLiveAnalysisEvent(true);

        executor.DidNotReceiveWithAnyArgs().Debounce(default, default);
    }

    [TestMethod]
    public void HandleBackgroundAnalysisEvent_ShouldDebounceAnalyzeFile_WhenIsWaiting()
    {
        executor.IsScheduled.Returns(false);
        SetUpExecuteImmediately();
        var fileSnapshot = CreateFileSnapshot();
        fileState.UpdateFileState().Returns(fileSnapshot);

        testSubject.HandleBackgroundAnalysisEvent();

        Received.InOrder(() =>
        {
            executor.Debounce(Arg.Any<Action<CancellationToken>>(), LiveAnalysisState.BackgroundAnalysisDebounceDuration);
            fileState.UpdateFileState();
            fileTracker.AddFiles(Arg.Is<SourceFile[]>(s => s.Single().FilePath == FilePath && s.Single().Content == Content));
        });
    }

    [TestMethod]
    public void HandleBackgroundAnalysisEvent_ShouldNotDebounce_WhenNotWaiting()
    {
        executor.IsScheduled.Returns(true);

        testSubject.HandleBackgroundAnalysisEvent();

        executor.DidNotReceiveWithAnyArgs().Debounce(default, default);
        fileState.DidNotReceiveWithAnyArgs().UpdateFileState();
        fileTracker.DidNotReceiveWithAnyArgs().AddFiles();
    }

    [TestMethod]
    public void Dispose_ShouldBeIdempotent()
    {
        testSubject.Dispose();
        testSubject.Dispose();

        executor.Received(1).Dispose();
    }

    private FileSnapshot CreateFileSnapshot()
    {
        var textSnapshot = Substitute.For<ITextSnapshot>();
        textSnapshot.GetText().Returns(Content);
        return new FileSnapshot(FilePath, textSnapshot);
    }
}
