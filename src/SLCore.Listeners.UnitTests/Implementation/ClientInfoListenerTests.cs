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
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class ClientInfoListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ClientInfoListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ISolutionInfoProvider>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ClientInfoListener>();
    }
    
    [TestMethod]
    public async Task GetClientLiveInfoAsync_ReturnsDescriptionFromStatus()
    {
        var solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
        const string solutionName = "mysolutionname";
        solutionInfoProvider.GetSolutionNameAsync().Returns(solutionName);
        var testSubject = new ClientInfoListener(solutionInfoProvider);

        var response = await testSubject.GetClientLiveInfoAsync();

        response.description.Should().BeSameAs(solutionName);
    }
}
