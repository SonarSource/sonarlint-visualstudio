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
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class SolutionWorkspaceServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SolutionWorkspaceService, ISolutionWorkspaceService>(
                MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IVsUIServiceOperation>());
        }

        [TestMethod]
        public void CheckIsSharedMefComponent()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SolutionWorkspaceService>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsSolutionWorkSpace_ShouldBeOppsiteOfFolderWorkSpace(bool isFolderSpace)
        {
            var solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
            solutionInfoProvider.IsFolderWorkspace().Returns(isFolderSpace);

            var testSubject = new SolutionWorkspaceService(solutionInfoProvider, new TestLogger(), Substitute.For<IVsUIServiceOperation>());

            var result = testSubject.IsSolutionWorkSpace();

            result.Should().Be(!isFolderSpace);
        }
    }
}
