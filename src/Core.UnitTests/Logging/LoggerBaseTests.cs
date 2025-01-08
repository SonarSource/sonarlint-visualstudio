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

using SonarLint.VisualStudio.Core.Logging;

namespace SonarLint.VisualStudio.Core.UnitTests.Logging;

[TestClass]
public class LoggerBaseTests
{
    private ILoggerContextManager contextManager;
    private ILoggerWriter writer;
    private ILoggerSettingsProvider settingsProvider;
    private LoggerBase testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        contextManager = Substitute.For<ILoggerContextManager>();
        writer = Substitute.For<ILoggerWriter>();
        settingsProvider = Substitute.For<ILoggerSettingsProvider>();
        testSubject = new LoggerBase(contextManager, writer, settingsProvider);
    }

    [TestMethod]
    public void ForContext_CreatesNewLoggerWithUpdatedContextManager()
    {
        var newContextManager = Substitute.For<ILoggerContextManager>();
        contextManager.CreateAugmentedContext(Arg.Any<IEnumerable<string>>()).Returns(newContextManager);

        var newLogger = testSubject.ForContext("ctx");

        contextManager.Received(1).CreateAugmentedContext(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new[] { "ctx" })));
        newLogger.Should().NotBeSameAs(testSubject);
        newLogger.WriteLine("msg");
        contextManager.DidNotReceiveWithAnyArgs().GetFormattedContextOrNull(default);
        newContextManager.ReceivedWithAnyArgs().GetFormattedContextOrNull(default);
        writer.Received().WriteLine(Arg.Any<string>());
        _ = settingsProvider.Received().IsVerboseEnabled;
    }

    [TestMethod]
    public void ForVerboseContext_CreatesNewLoggerWithUpdatedContextManager()
    {
        var newContextManager = Substitute.For<ILoggerContextManager>();
        contextManager.CreateAugmentedVerboseContext(Arg.Any<IEnumerable<string>>()).Returns(newContextManager);

        var newLogger = testSubject.ForVerboseContext("ctx");

        contextManager.Received(1).CreateAugmentedVerboseContext(Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(new[] { "ctx" })));
        newLogger.Should().NotBeSameAs(testSubject);
        newLogger.WriteLine("msg");
        contextManager.DidNotReceiveWithAnyArgs().GetFormattedContextOrNull(default);
        newContextManager.ReceivedWithAnyArgs().GetFormattedContextOrNull(default);
        writer.Received().WriteLine(Arg.Any<string>());
        _ = settingsProvider.Received().IsVerboseEnabled;
    }

    [TestMethod]
    public void LogVerbose_VerboseDisabled_DoesNothing()
    {
        settingsProvider.IsVerboseEnabled.Returns(false);

        testSubject.LogVerbose("msg {0}", "sent");

        writer.DidNotReceiveWithAnyArgs().WriteLine(default);
    }

    [TestMethod]
    public void LogVerbose_VerboseEnabled_AddsDebugProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);

        testSubject.LogVerbose("msg {0}", "sent");

        writer.Received().WriteLine("[DEBUG] msg sent");
    }

    [TestMethod]
    public void LogVerbose_ThreadIdLoggingEnabled_AddsThreadIdProperty()
    {
        settingsProvider.IsThreadIdEnabled.Returns(true);
        settingsProvider.IsVerboseEnabled.Returns(true);

        testSubject.LogVerbose("msg {0}", "sent");

        writer.Received().WriteLine($"[DEBUG] [ThreadId {Thread.CurrentThread.ManagedThreadId}] msg sent");
    }

    [TestMethod]
    public void LogVerbose_Context_AddsContextProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedContextOrNull(default).Returns("context");

        testSubject.LogVerbose("msg {0}", "sent");

        writer.Received().WriteLine("[DEBUG] [context] msg sent");
    }

    [TestMethod]
    public void LogVerbose_VerboseContext_AddsVerboseContextProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.LogVerbose("msg {0}", "sent");

        writer.Received().WriteLine("[DEBUG] [verbose context] msg sent");
    }

    [TestMethod]
    public void LogVerbose_AllContextsEnabled_AddsInCorrectOrder()
    {
        settingsProvider.IsThreadIdEnabled.Returns(true);
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedContextOrNull(default).Returns("context");
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.LogVerbose("msg {0}", "sent");

        writer.Received().WriteLine($"[DEBUG] [ThreadId {Thread.CurrentThread.ManagedThreadId}] [context] [verbose context] msg sent");
    }

    [TestMethod]
    public void LogVerboseWithContext_AllContextsEnabled_AddsInCorrectOrder()
    {
        var messageLevelContext = new MessageLevelContext
        {
            Context = Substitute.For<IReadOnlyCollection<string>>(),
            VerboseContext = Substitute.For<IReadOnlyCollection<string>>()
        };
        settingsProvider.IsThreadIdEnabled.Returns(true);
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedContextOrNull(messageLevelContext).Returns("context with message level");
        contextManager.GetFormattedVerboseContextOrNull(messageLevelContext).Returns("verbose context with message level");

        testSubject.LogVerbose(messageLevelContext, "msg {0}", "sent");

        writer.Received().WriteLine($"[DEBUG] [ThreadId {Thread.CurrentThread.ManagedThreadId}] [context with message level] [verbose context with message level] msg sent");
    }

    [TestMethod]
    public void WriteLine_VerboseDisabled_Writes()
    {
        settingsProvider.IsVerboseEnabled.Returns(false);

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine("msg sent");
    }

    [TestMethod]
    public void WriteLineFormatted_VerboseDisabled_Writes()
    {
        settingsProvider.IsVerboseEnabled.Returns(false);

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine("msg sent");
    }

    [TestMethod]
    public void WriteLine_VerboseEnabled_DoesNotAddDebugProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine("msg sent");
    }

    [TestMethod]
    public void WriteLineFormatted_VerboseEnabled_DoesNotAddDebugProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine("msg sent");
    }

    [TestMethod]
    public void WriteLine_ThreadIdLoggingEnabled_AddsThreadIdProperty()
    {
        settingsProvider.IsThreadIdEnabled.Returns(true);

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine($"[ThreadId {Thread.CurrentThread.ManagedThreadId}] msg sent");
    }

    [TestMethod]
    public void WriteLineFormatted_ThreadIdLoggingEnabled_AddsThreadIdProperty()
    {
        settingsProvider.IsThreadIdEnabled.Returns(true);

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine($"[ThreadId {Thread.CurrentThread.ManagedThreadId}] msg sent");
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void WriteLine_Context_AddsContextProperty(bool isVerboseEnabled)
    {
        settingsProvider.IsVerboseEnabled.Returns(isVerboseEnabled);
        contextManager.GetFormattedContextOrNull(default).Returns("context");

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine("[context] msg sent");
    }

    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void WriteLineFormatted_Context_AddsContextProperty(bool isVerboseEnabled)
    {
        settingsProvider.IsVerboseEnabled.Returns(isVerboseEnabled);
        contextManager.GetFormattedContextOrNull(default).Returns("context");

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine("[context] msg sent");
    }

    [TestMethod]
    public void WriteLine_VerboseContext_VerboseLoggingDisabled_DoesNotAddVerboseContextProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(false);
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine("msg sent");
    }

    [TestMethod]
    public void WriteLineFormatted_VerboseContext_VerboseLoggingDisabled_DoesNotAddVerboseContextProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(false);
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine("msg sent");
    }

    [TestMethod]
    public void WriteLine_VerboseContext_VerboseLoggingEnabled_AddsVerboseContextProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine("[verbose context] msg sent");
    }

    [TestMethod]
    public void WriteLineFormatted_VerboseContext_VerboseLoggingEnabled_AddsVerboseContextProperty()
    {
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine("[verbose context] msg sent");
    }

    [TestMethod]
    public void WriteLine_AllContextsEnabled_AddsInCorrectOrder()
    {
        settingsProvider.IsThreadIdEnabled.Returns(true);
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedContextOrNull(default).Returns("context");
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.WriteLine("msg sent");

        writer.Received().WriteLine($"[ThreadId {Thread.CurrentThread.ManagedThreadId}] [context] [verbose context] msg sent");
    }

    [TestMethod]
    public void WriteLineFormatted_AllContextsEnabled_AddsInCorrectOrder()
    {
        settingsProvider.IsThreadIdEnabled.Returns(true);
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedContextOrNull(default).Returns("context");
        contextManager.GetFormattedVerboseContextOrNull(default).Returns("verbose context");

        testSubject.WriteLine("msg {0}", "sent");

        writer.Received().WriteLine($"[ThreadId {Thread.CurrentThread.ManagedThreadId}] [context] [verbose context] msg sent");
    }

    [TestMethod]
    public void WriteLineFormattedWithContext_AllContextsEnabled_AddsInCorrectOrder()
    {
        var messageLevelContext = new MessageLevelContext
        {
            Context = Substitute.For<IReadOnlyCollection<string>>(),
            VerboseContext = Substitute.For<IReadOnlyCollection<string>>()
        };
        settingsProvider.IsThreadIdEnabled.Returns(true);
        settingsProvider.IsVerboseEnabled.Returns(true);
        contextManager.GetFormattedContextOrNull(messageLevelContext).Returns("context with message level");
        contextManager.GetFormattedVerboseContextOrNull(messageLevelContext).Returns("verbose context with message level");

        testSubject.WriteLine(messageLevelContext, "msg {0}", "sent");

        writer.Received().WriteLine($"[ThreadId {Thread.CurrentThread.ManagedThreadId}] [context with message level] [verbose context with message level] msg sent");
    }
}
