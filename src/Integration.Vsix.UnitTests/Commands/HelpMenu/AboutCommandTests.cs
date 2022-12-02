/*
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Commands;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class AboutCommandTests
    {
        [TestMethod]
        public void Invoke_VerifyCallsBrowserServiceNavigate()
        {
            OleMenuCommand command = CommandHelper.CreateRandomOleMenuCommand();

            var browserService = new Mock<IBrowserService>();

            var testSubject = new AboutCommand(browserService.Object);

            var aboutUrl = "https://marketplace.visualstudio.com/items?itemName=SonarSource.SonarLintforVisualStudio2022";

            browserService.Verify(x => x.Navigate(aboutUrl), Times.Never);
            testSubject.Invoke(command, null);
            browserService.Verify(x => x.Navigate(aboutUrl), Times.Once);
        }
    }
}
