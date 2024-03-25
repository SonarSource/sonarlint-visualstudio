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

using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Core;

[TestClass]
public class RpcDebuggerTests
{
    private const string SONARLINT_LOG_RPC = "SONARLINT_LOG_RPC";

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<RpcDebugger, IRpcDebugger>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RpcDebugger>();
    }

    [TestMethod]
    public void CreateDebugOutput_EnvVarNotSetTrue_DoesNothing()
    {
        using var environment = new EnvironmentVariableHelper(SONARLINT_LOG_RPC, null, EnvironmentVariableTarget.Process);
        var fileSystem = CreateFileSystem();
        var jsonRpc = Substitute.For<IJsonRpc>();

        var testSubject = new RpcDebugger(fileSystem, DateTime.Now);
        testSubject.SetUpDebugger(jsonRpc);

        fileSystem.ReceivedCalls().Should().BeEmpty();
        jsonRpc.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void CreateDebugOutput_EnvVarSetTrue_EnablesTracing()
    {
        using var environment = new EnvironmentVariableHelper(SONARLINT_LOG_RPC, "true", EnvironmentVariableTarget.Process);
        SetUpFileSystem(out var filePath, out var fileSystem, out var fileStreamFactory, out var fileDate);
        SetUpJsonRpc(out var jsonRpc, out var traceSource);

        var testSubject = new RpcDebugger(fileSystem, fileDate);
        testSubject.SetUpDebugger(jsonRpc);

        fileStreamFactory.Received(1).Create(filePath, FileMode.Create);
        _ = jsonRpc.Received().TraceSource;
        traceSource.Listeners.Should().HaveCount(1);
        traceSource.Switch.Level.Should().Be(SourceLevels.Verbose);
    }
    
    [TestMethod]
    public void CreateDebugOutput_SetUpMultipleTimes_CreatesStreamOnce()
    {
        using var environment = new EnvironmentVariableHelper(SONARLINT_LOG_RPC, "true", EnvironmentVariableTarget.Process);
        SetUpFileSystem(out var filePath, out var fileSystem, out var fileStreamFactory, out var fileDate);
        SetUpJsonRpc(out var jsonRpc, out var traceSource);
        SetUpJsonRpc(out var jsonRpcNew, out var traceSourceNew);

        var testSubject = new RpcDebugger(fileSystem, fileDate);
        testSubject.SetUpDebugger(jsonRpc);
        testSubject.SetUpDebugger(jsonRpcNew);

        fileStreamFactory.Received(1).Create(filePath, FileMode.Create);
        _ = jsonRpc.Received().TraceSource;
        traceSource.Listeners.Should().HaveCount(1);
        traceSource.Switch.Level.Should().Be(SourceLevels.Verbose);
        _ = jsonRpcNew.Received().TraceSource;
        traceSourceNew.Listeners.Should().HaveCount(1);
        traceSourceNew.Switch.Level.Should().Be(SourceLevels.Verbose);
    }

    [TestMethod]
    public void CreateDebugOutput_FileOverrideSet_UsesOverridenPath()
    {
        using var environment = new EnvironmentVariableHelper(SONARLINT_LOG_RPC, "true", EnvironmentVariableTarget.Process);
        var fileStreamFactory = Substitute.For<IFileStreamFactory>();
        var jsonRpc = Substitute.For<IJsonRpc>();
        var traceSource = Substitute.ForPartsOf<TraceSource>("test");
        var stream = Substitute.For<Stream>();
        IFileSystem fileSystem = CreateFileSystem();
        fileSystem.FileStream.Returns(fileStreamFactory);
        jsonRpc.TraceSource.Returns(traceSource);
        fileStreamFactory.Create("C:\\test.log", FileMode.Create).Returns(stream);
        stream.CanWrite.Returns(true);

        var testSubject = new RpcDebugger(fileSystem, "C:\\test.log");
        testSubject.SetUpDebugger(jsonRpc);

        fileStreamFactory.Received(1).Create("C:\\test.log", FileMode.Create);
    }
    
    private static void SetUpJsonRpc(out IJsonRpc jsonRpc, out TraceSource traceSource)
    {
        jsonRpc = Substitute.For<IJsonRpc>();
        traceSource = Substitute.ForPartsOf<TraceSource>("test");
        traceSource.Listeners.Clear();
        jsonRpc.TraceSource.Returns(traceSource);
    }

    private static void SetUpFileSystem(out string filePath, out IFileSystem fileSystem, out IFileStreamFactory fileStreamFactory, out DateTime fileDate)
    {
        var logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SonarLint for Visual Studio",
            "Rpc Logs");
        fileDate = new DateTime(2024, 3, 1, 11, 22, 33);
        filePath = Path.Combine(logsFolder, "2024-03-01_1122330000.log");
        fileSystem = CreateFileSystem();
        fileStreamFactory = Substitute.For<IFileStreamFactory>();
        var stream = Substitute.For<Stream>();
        stream.CanWrite.Returns(true);
        fileSystem.FileStream.Returns(fileStreamFactory);
        fileStreamFactory.Create(filePath, FileMode.Create).Returns(stream);
    }

    private static IFileSystem CreateFileSystem()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var directory = Substitute.For<IDirectory>();
        fileSystem.Directory.Returns(directory);
        return fileSystem;
    }

    private class EnvironmentVariableHelper : IDisposable
    {
        private readonly string variable;
        private readonly EnvironmentVariableTarget target;

        public EnvironmentVariableHelper(string variable,
            string value,
            EnvironmentVariableTarget target)
        {
            this.variable = variable;
            this.target = target;
            Environment.SetEnvironmentVariable(variable, value, target);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(variable, null, target);
        }
    }
}
