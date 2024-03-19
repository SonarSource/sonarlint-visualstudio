﻿/*
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
    public void CreateDebugOutput_NoFileSet_DoesNothing()
    {
        var testSubject = CreateTestSubject(out var fileSystem);
        var jsonRpc = Substitute.For<IJsonRpc>();

        testSubject.SetUpDebugger(jsonRpc);

        fileSystem.ReceivedCalls().Should().BeEmpty();
        jsonRpc.ReceivedCalls().Should().BeEmpty();
    }
    
    [TestMethod]
    public void CreateDebugOutput_FilePathSet_EnablesTracing()
    {
        const string filePath = "file/path";
        var fileStreamFactory = Substitute.For<IFileStreamFactory>();
        var stream = Substitute.For<Stream>();
        var jsonRpc = Substitute.For<IJsonRpc>();
        var traceSource = Substitute.ForPartsOf<TraceSource>("test");
        var testSubject = CreateTestSubject(out var fileSystem);
        fileSystem.FileStream.Returns(fileStreamFactory);
        fileStreamFactory.Create(filePath, FileMode.Create).Returns(stream);
        stream.CanWrite.Returns(true);
        traceSource.Listeners.Clear();
        jsonRpc.TraceSource.Returns(traceSource);

        testSubject.LogFilePath = filePath;
        testSubject.SetUpDebugger(jsonRpc);

        fileStreamFactory.Received(1).Create(filePath, FileMode.Create);
        _ = jsonRpc.Received().TraceSource;
        traceSource.Listeners.Should().HaveCount(1);
        traceSource.Switch.Level.Should().Be(SourceLevels.Verbose);
    }

    private IRpcDebugger CreateTestSubject(out IFileSystem fileSystem)
    {
        fileSystem = Substitute.For<IFileSystem>();
        return new RpcDebugger(fileSystem);
    }
}
