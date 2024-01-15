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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl.ViewModelCommands
{
    [TestClass]
    public class NavigateToDocumentationCommandTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(0)]
        [DataRow(false)]
        public void CanExecute_True(object parameter)
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.CanExecute(parameter);

            result.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(0)]
        [DataRow(false)]
        public void Execute_DocumentationShown(object parameter)
        {
            var browserService = new Mock<IShowInBrowserService>();
            var testSubject = CreateTestSubject(browserService.Object);

            testSubject.Execute(parameter);

            browserService.Verify(x=> x.ShowDocumentation(), Times.Once);
            browserService.VerifyNoOtherCalls();
        }

        private NavigateToDocumentationCommand CreateTestSubject(IShowInBrowserService browserService = null)
        {
            return new NavigateToDocumentationCommand(browserService);
        }
    }
}
