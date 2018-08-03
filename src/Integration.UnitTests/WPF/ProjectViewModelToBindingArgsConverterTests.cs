/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Globalization;
using System.Security;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class ProjectViewModelToBindingArgsConverterTests
    {
        [TestMethod]
        public void Convert_NotProjectViewModel_ReturnsNull()
        {
            // Arrange
            var converter = new ProjectViewModelToBindingArgsConverter();

            // Act && Assert
            converter.Convert(null, typeof(object), null, CultureInfo.CurrentCulture).Should().BeNull();
            converter.Convert("a string", typeof(object), null, CultureInfo.CurrentCulture).Should().BeNull();
        }

        [TestMethod]
        public void Convert_ValidProjecModel_ReturnsBindingArgs()
        {
            // Arrange
            var expectedUri = new Uri("http://localhost:9000");
            var expectedPassword = new SecureString();
            expectedPassword.AppendChar('x');
            var serverViewModel = new ServerViewModel(new ConnectionInformation(expectedUri, "user1", expectedPassword));
            var project = new SonarQubeProject("key1", "name1");
            var projectViewModel = new ProjectViewModel(serverViewModel, project);

            var converter = new ProjectViewModelToBindingArgsConverter();

            // Act
            var convertedObj = converter.Convert(projectViewModel, typeof(object), null, System.Globalization.CultureInfo.CurrentCulture);
            convertedObj.Should().NotBeNull();
            convertedObj.Should().BeOfType<BindCommandArgs>();

            var bindCommandArgs = (BindCommandArgs)convertedObj;
            bindCommandArgs.ProjectKey.Should().Be("key1");
            bindCommandArgs.ProjectName.Should().Be("name1");
            bindCommandArgs.Connection.Should().NotBeNull();
            bindCommandArgs.Connection.ServerUri.Should().BeSameAs(expectedUri);
            bindCommandArgs.Connection.UserName.Should().Be("user1");
            bindCommandArgs.Connection.Password.Length.Should().Be(1);
        }
    }
}
