/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.TsConfig;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.TsConfig
{
    [TestClass]
    public class TsConfigsLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TsConfigsLocator, ITsConfigsLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<IFilePathsLocator>(Mock.Of<IFilePathsLocator>()),
            });
        }

        [TestMethod]
        public void Locate_CallsFilePathsLocator()
        {
            var locatedFiles = new[] {"tsconfig1", "tsconfig2" };
            var hierarchy = Mock.Of<IVsHierarchy>();

            var filePathsLocator = new Mock<IFilePathsLocator>();
            filePathsLocator.Setup(x => x.Locate(hierarchy, "tsconfig.json")).Returns(locatedFiles);

            var testSubject = CreateTestSubject(filePathsLocator.Object);
            var result = testSubject.Locate(hierarchy);

            result.Should().BeEquivalentTo(locatedFiles);

            filePathsLocator.Verify(x=> x.Locate(hierarchy, "tsconfig.json"), Times.Once);
            filePathsLocator.VerifyNoOtherCalls();
        }

        private TsConfigsLocator CreateTestSubject(IFilePathsLocator filePathsLocator) => new TsConfigsLocator(filePathsLocator);
    }
}
