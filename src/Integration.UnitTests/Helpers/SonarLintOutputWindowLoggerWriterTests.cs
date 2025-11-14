/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers;

[TestClass]
public class SonarLintOutputWindowLoggerWriterTests
{
    private ConfigurableVsOutputWindow windowMock;
    private IServiceProvider serviceProviderMock;
    private SonarLintOutputWindowLoggerWriter testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        windowMock = new ConfigurableVsOutputWindow();
        serviceProviderMock = CreateConfiguredServiceProvider(windowMock);
        testSubject = new SonarLintOutputWindowLoggerWriter(serviceProviderMock);
    }

    [TestMethod]
    public void WriteLine_Empty_PutsEmptyLineToCorrectPane()
    {
        var message = string.Empty;

        testSubject.WriteLine(message);

        var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
        outputPane.AssertOutputStrings(message);
    }

    [TestMethod]
    public void WriteLine_Simple_PutsSingleLineToCorrectPane()
    {
        var message = "ABOBA";

        testSubject.WriteLine(message);

        var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
        outputPane.AssertOutputStrings(message);
    }

    [TestMethod]
    public void WriteLine_Multiline_PutsMultiLineToCorrectPane()
    {
        var message =
            """
            A
            B
            OBA
            """;

        testSubject.WriteLine(message);

        var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
        outputPane.AssertOutputStrings(message);
    }

    private static IServiceProvider CreateConfiguredServiceProvider(IVsOutputWindow outputWindow)
    {
        var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
        serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
        return serviceProvider;
    }
}
