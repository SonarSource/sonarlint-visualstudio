﻿/*
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

using System;
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.Settings
{
    [TestClass]
    public class GeneralOptionsDialogControlTests
    {
        private readonly ICommand ValidCommand = new Mock<ICommand>().Object;

        [TestMethod]
        public void Ctor_WithValidCommand_DoesNotThrow()
        {
            Action act = () => new GeneralOptionsDialogControl(ValidCommand, ValidCommand);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void Ctor_NullArgs_ThrowsArgumentNullException()
        {
            Action act = () => new GeneralOptionsDialogControl(null,  ValidCommand);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("openSettingsFileCommand");

            act = () => new GeneralOptionsDialogControl(ValidCommand, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("showWikiCommand");
        }
    }
}
