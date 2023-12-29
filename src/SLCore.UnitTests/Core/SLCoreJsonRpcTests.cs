/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Threading.Tasks;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.UnitTests.Helpers;
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
        var methodNameTransformerMock = new Mock<IRpcMethodNameTransformer>();
        Func<string, string> transformer = s => s;
        methodNameTransformerMock.Setup(x => x.Create<ITestSLCoreService>()).Returns(transformer);
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
        var testSubject = CreateTestSubject(out var clientMock, out _);
        var listener = new TestSLCoreListener();
        clientMock.Setup(x => x.AddLocalRpcTarget(listener, It.IsAny<JsonRpcTargetOptions>()));

        testSubject.AttachListener(listener);
        
        clientMock.Verify(x => x.AddLocalRpcTarget(listener, It.Is<JsonRpcTargetOptions>(options =>
                options.MethodNameTransform == CommonMethodNameTransforms.CamelCase && options.UseSingleObjectParameterDeserialization)),
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
    
    private static SLCoreJsonRpc CreateTestSubject(out Mock<IJsonRpc> clientMock,
        out TaskCompletionSource<bool> clientCompletionSource,
        IRpcMethodNameTransformer methodNameTransformer = null)
    {
        (clientMock, clientCompletionSource) = TestJsonRpcFactory.Create();
        return new SLCoreJsonRpc(clientMock.Object, methodNameTransformer);
    }
    
    public interface ITestSLCoreService : ISLCoreService {}
    
    public class TestSLCoreListener : ISLCoreListener {}
}
