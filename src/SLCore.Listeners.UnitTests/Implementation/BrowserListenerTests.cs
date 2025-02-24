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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Listener.Visualization;
using SonarLint.VisualStudio.SLCore.Listener.Visualization.Models;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class BrowserListenerTests
{
    private IBrowserService browserService;
    private BrowserListener testSubject;

    [TestInitialize]
    public void SetUp()
    {
        browserService = Substitute.For<IBrowserService>();
        testSubject = new BrowserListener(browserService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<BrowserListener, IBrowserListener>(
            MefTestHelpers.CreateExport<IBrowserService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<BrowserListener>();

    [TestMethod]
    public void OpenUrlInBrowser_NavigatesToUrl()
    {
        var url = "http://example.com";

        testSubject.OpenUrlInBrowser(new OpenUrlInBrowserParams(url));

        browserService.Received(1).Navigate(url);
    }
}
