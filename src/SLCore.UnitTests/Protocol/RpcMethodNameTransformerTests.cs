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

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.TestInfrastructure;
using StreamJsonRpc;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Protocol;

[TestClass]
public class RpcMethodNameTransformerTests
{
    private const string Prefix = "prefix";
    
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<RpcMethodNameTransformer, IRpcMethodNameTransformer>();
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<RpcMethodNameTransformer>();
    }
    
    [TestMethod]
    public void Create_NoAttribute_CreatesCamelCaseTransformer()
    {
        var testSubject = CreateTestSubject();

        var transformer = testSubject.Create<ITestSLCoreServiceNoAttribute>();

        transformer.Should().BeSameAs(CommonMethodNameTransforms.CamelCase);
    }
    
    [DataTestMethod]
    [DataRow("Method", $"{Prefix}/method")]
    [DataRow("MyMethod", $"{Prefix}/myMethod")]
    [DataRow("MyLovelyMethod", $"{Prefix}/myLovelyMethod")]
    [DataRow("myLovelyMethod", $"{Prefix}/myLovelyMethod")]
    [DataRow("mylovelymethod", $"{Prefix}/mylovelymethod")]
    public void Create_NoAttribute_CreatesCompositeTransformer(string methodName, string expectedMethodName)
    {
        var testSubject = CreateTestSubject();

        var transformer = testSubject.Create<ITestSLCoreServiceWithAttribute>();

        transformer(methodName).Should().Be(expectedMethodName);
    }

    private RpcMethodNameTransformer CreateTestSubject()
    {
        return new RpcMethodNameTransformer();
    }
    
    public interface ITestSLCoreServiceNoAttribute : ISLCoreService {}
    
    [JsonRpcClass(Prefix)]
    public interface ITestSLCoreServiceWithAttribute : ISLCoreService {}
}
