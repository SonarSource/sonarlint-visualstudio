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

using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using StreamJsonRpc;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Core;

[TestClass]
public class SLCoreJsonRpcTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void IsAlive_UpdatesOnCompletion(bool triggerCompletion)
    {
        var testSubject = CreateTestSubject(out var clientMock, out var completionSource);

        testSubject.IsAlive.Should().BeTrue();

        if (triggerCompletion)
        {
            completionSource.TrySetResult(true);
            testSubject.IsAlive.Should().BeFalse();
        }
        else
        {
            testSubject.IsAlive.Should().BeTrue();
        }
        
        clientMock.VerifyGet(x => x.Completion, Times.Exactly(2));
    }
    
    [TestMethod]
    public void IsAlive_TaskCanceled_NoException()
    {
        var testSubject = CreateTestSubject(out var clientMock, out var completionSource);

        testSubject.IsAlive.Should().BeTrue();

        completionSource.TrySetCanceled();
        
        testSubject.IsAlive.Should().BeFalse();
        clientMock.VerifyGet(x => x.Completion, Times.Exactly(2));
    }
    
    [TestMethod]
    public void IsAlive_TaskThrows_NoException()
    {
        var testSubject = CreateTestSubject(out var clientMock, out var completionSource);

        testSubject.IsAlive.Should().BeTrue();

        completionSource.TrySetException(new Exception());
        
        testSubject.IsAlive.Should().BeFalse();
        clientMock.VerifyGet(x => x.Completion, Times.Exactly(2));
    }
    
    [TestMethod]
    public void CreateService_CallsAttachWithCorrectOptions()
    {
        var methodNameTransformerMock = CreateMethodNameTransformerMock<ITestSLCoreService>(out var transformer);
        var testSubject = CreateTestSubject(out var clientMock, out _, methodNameTransformerMock.Object);
        var service = Mock.Of<ITestSLCoreService>();
        clientMock.Setup(x => x.Attach<ITestSLCoreService>(It.IsAny<JsonRpcProxyOptions>())).Returns(service);

        var createdService = testSubject.CreateService<ITestSLCoreService>();

        createdService.Should().BeSameAs(service);
        clientMock.Verify(x =>
                x.Attach<ITestSLCoreService>(It.Is<JsonRpcProxyOptions>(options =>
                    options.MethodNameTransform == transformer)),
            Times.Once);
        clientMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void AttachListener_CallsAddLocalRpcTargetWithCorrectOptions()
    {
        var methodNameTransformerMock = CreateMethodNameTransformerMock<ISLCoreListener>(out var transformer);
        var testSubject = CreateTestSubject(out var clientMock, out _, methodNameTransformerMock.Object);
        var listener = new TestSLCoreListener();
        clientMock.Setup(x => x.AddLocalRpcTarget(listener, It.IsAny<JsonRpcTargetOptions>()));

        testSubject.AttachListener(listener);
        
        clientMock.Verify(x => x.AddLocalRpcTarget(listener, It.Is<JsonRpcTargetOptions>(options =>
                options.MethodNameTransform == transformer && options.UseSingleObjectParameterDeserialization)),
            Times.Once);
        clientMock.VerifyNoOtherCalls();
    }
    
    [TestMethod]
    public void StartListening_CallsJsonrpcStartListening()
    {
        var testSubject = CreateTestSubject(out var clientMock, out _);
        
        testSubject.StartListening();
        
        clientMock.Verify(x => x.StartListening(), Times.Once);
    }
    
    private static ISLCoreJsonRpc CreateTestSubject(out Mock<IJsonRpc> clientMock,
        out TaskCompletionSource<bool> clientCompletionSource,
        IRpcMethodNameTransformer methodNameTransformer = null)
    {
        (clientMock, clientCompletionSource) = CreateJsonRpc();
        return new SLCoreJsonRpcFactory(methodNameTransformer).CreateSLCoreJsonRpc(clientMock.Object);
    }

    private static Mock<IRpcMethodNameTransformer> CreateMethodNameTransformerMock<T>(out Func<string, string> transformer)
    {
        var methodNameTransformerMock = new Mock<IRpcMethodNameTransformer>();
        transformer = s => s;
        methodNameTransformerMock.Setup(x => x.Create<T>()).Returns(transformer);
        return methodNameTransformerMock;
    }
    
    private static (Mock<IJsonRpc> clientMock, TaskCompletionSource<bool> clientCompletionSource) CreateJsonRpc()
    {
        var mock = new Mock<IJsonRpc>();
        var tcs = new TaskCompletionSource<bool>();
        mock.SetupGet(x => x.Completion).Returns(tcs.Task);
        return (mock, tcs);
    }
    
    public interface ITestSLCoreService : ISLCoreService {}
    
    public class TestSLCoreListener : ISLCoreListener {}
}
