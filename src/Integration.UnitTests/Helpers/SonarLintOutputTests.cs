﻿/*
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class SonarLintOutputTests
    {
        [TestMethod]
        public void Ctor_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SonarLintOutput(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var spMock = new Mock<IServiceProvider>();
            var spExport = MefTestHelpers.CreateExport<SVsServiceProvider>(spMock.Object);

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<SonarLintOutput, ISonarLintOutput>(null, new[] { spExport });
        }

        [TestMethod]
        public void Write_OutputsToWindow()
        {
            // Arrange
            var windowMock = new ConfigurableVsOutputWindow();
            
            var serviceProviderMock = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
            serviceProviderMock.RegisterService(typeof(SVsOutputWindow), windowMock);

            SonarLintOutput logger = new SonarLintOutput(serviceProviderMock);

            // Act
            logger.Write("123");
            logger.Write("abc");

            // Assert
            var outputPane = windowMock.AssertPaneExists(VsShellUtils.SonarLintOutputPaneGuid);
            outputPane.AssertOutputStrings("123", "abc");
        }
    }
}
