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

using NSubstitute;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;

namespace SonarLint.VisualStudio.SLCore.UnitTests;

[TestClass]
public class SLCoreRpcFactoryTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreRpcFactory, ISLCoreRpcFactory>(
            MefTestHelpers.CreateExport<ISLCoreProcessFactory>(),
            MefTestHelpers.CreateExport<ISLCoreLocator>(),
            MefTestHelpers.CreateExport<ISLCoreJsonRpcFactory>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProviderWriter>(),
            MefTestHelpers.CreateExport<ISLCoreListenerSetUp>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreRpcFactory>();
    }

    [TestMethod]
    public void StartNewRpcInstance_CorrectlyComposesSLCoreRpc()
    {
        var testSubject = CreateTestSubject(out var slCoreProcessFactory, 
            out var slCoreLocator,
            out var slCoreJsonRpcFactory,
            out var slCoreServiceProviderWriter,
            out var slCoreListenerSetUp);

        SetUpDependencies(slCoreLocator,
            slCoreProcessFactory,
            slCoreJsonRpcFactory,
            out var slCoreLaunchParameters,
            out var slCoreProcess,
            out var jsonRpc,
            out var slCoreJsonRpc);

        testSubject.StartNewRpcInstance().Should().NotBeNull();

        Received.InOrder(() =>
        {
            slCoreLocator.LocateExecutable();
            slCoreProcessFactory.StartNewProcess(slCoreLaunchParameters);
            slCoreProcess.AttachJsonRpc();
            slCoreJsonRpcFactory.CreateSLCoreJsonRpc(jsonRpc);
            slCoreServiceProviderWriter.SetCurrentConnection(slCoreJsonRpc);
            slCoreListenerSetUp.Setup(slCoreJsonRpc);
            slCoreJsonRpc.StartListening();
        });
    }

    [TestMethod]
    public void SLCoreRpc_DisposesProcess()
    {
        var testSubject = CreateTestSubject(out var slCoreProcessFactory, 
            out var slCoreLocator,
            out var slCoreJsonRpcFactory,
            out _,
            out _);

        SetUpDependencies(slCoreLocator,
            slCoreProcessFactory,
            slCoreJsonRpcFactory,
            out _,
            out var slCoreProcess,
            out _,
            out _);

        var slCoreRpc = testSubject.StartNewRpcInstance();
        slCoreRpc.Dispose();
        
        slCoreProcess.Received(1).Dispose();
    }

    private static void SetUpDependencies(ISLCoreLocator slCoreLocator,
        ISLCoreProcessFactory slCoreProcessFactory,
        ISLCoreJsonRpcFactory slCoreJsonRpcFactory, 
        out SLCoreLaunchParameters slCoreLaunchParameters, 
        out ISLCoreProcess slCoreProcess,
        out IJsonRpc jsonRpc,
        out ISLCoreJsonRpc slCoreJsonRpc)
    {
        slCoreLaunchParameters = new SLCoreLaunchParameters(default, default);
        slCoreLocator.LocateExecutable().Returns(slCoreLaunchParameters);
        slCoreProcess = Substitute.For<ISLCoreProcess>();
        slCoreProcessFactory.StartNewProcess(slCoreLaunchParameters).Returns(slCoreProcess);
        jsonRpc = Substitute.For<IJsonRpc>();
        slCoreProcess.AttachJsonRpc().Returns(jsonRpc);
        slCoreJsonRpc = Substitute.For<ISLCoreJsonRpc>();
        slCoreJsonRpcFactory.CreateSLCoreJsonRpc(jsonRpc).Returns(slCoreJsonRpc);
    }

    private static SLCoreRpcFactory CreateTestSubject(out ISLCoreProcessFactory slCoreProcessFactory, 
        out ISLCoreLocator slCoreLocator,
        out ISLCoreJsonRpcFactory slCoreJsonRpcFactory,
        out ISLCoreServiceProviderWriter slCoreServiceProviderWriter,
        out ISLCoreListenerSetUp slCoreListenerSetUp)
    {
        slCoreProcessFactory = Substitute.For<ISLCoreProcessFactory>();
        slCoreLocator = Substitute.For<ISLCoreLocator>();
        slCoreJsonRpcFactory = Substitute.For<ISLCoreJsonRpcFactory>();
        slCoreServiceProviderWriter = Substitute.For<ISLCoreServiceProviderWriter>();
        slCoreListenerSetUp = Substitute.For<ISLCoreListenerSetUp>();
        return new SLCoreRpcFactory(slCoreProcessFactory, slCoreLocator, slCoreJsonRpcFactory, slCoreServiceProviderWriter, slCoreListenerSetUp);
    }
}
