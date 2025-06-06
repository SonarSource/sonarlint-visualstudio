﻿/*
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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Commands;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands
{
    [TestClass]
    public class OptionsCommandTests
    {
        [TestMethod]
        public void Invoke_VerifyCallsShowOptionsOnce()
        {
            var showOptionsCallback = new Mock<PackageCommandManager.ShowOptionsPage>();

            var testSubject = new OptionsCommand(showOptionsCallback.Object);

            showOptionsCallback.Verify(x => x(It.IsAny<Type>()), Times.Never);

            var dummyCommand = CommandHelper.CreateRandomOleMenuCommand();
            testSubject.Invoke(dummyCommand, null);

            showOptionsCallback.Verify(x => x(typeof(GeneralOptionsDialogPage)), Times.Once);
            showOptionsCallback.VerifyNoOtherCalls();
        }
    }
}
