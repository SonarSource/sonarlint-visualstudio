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

using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class GetFileExclusionsListenerTests
{
    private GetFileExclusionsListener testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new GetFileExclusionsListener();

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<GetFileExclusionsListener, ISLCoreListener>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ListFilesListener>();

    [TestMethod]
    public async Task GetFileExclusionsAsync_ValidParameters_ReturnsExpectedResponse()
    {
        var parameters = new GetFileExclusionsParams("testScopeId");

        var act = async () => await testSubject.GetFileExclusionsAsync(parameters);

        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
