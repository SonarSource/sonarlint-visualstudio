﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Commands.HelpMenu;
using SonarLint.VisualStudio.IssueVisualization.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands;

[TestClass]
public class ReportProblemCommandTests
{
    [TestMethod]
    public void ReportProblemCommand_Invoke()
    {
        var command = CommandHelper.CreateRandomOleMenuCommand();
        var showInBrowserService = new Mock<IShowInBrowserService>();
        var showLogsCommand = new ReportProblemCommand(showInBrowserService.Object);

        showInBrowserService.Verify(x => x.ShowProblemReportPage(), Times.Never);

        showLogsCommand.Invoke(command, null);

        showInBrowserService.Verify(x => x.ShowProblemReportPage(), Times.Once);
    }
}
