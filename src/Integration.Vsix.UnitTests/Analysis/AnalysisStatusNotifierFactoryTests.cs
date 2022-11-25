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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class AnalysisStatusNotifierFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<AnalysisStatusNotifierFactory, IAnalysisStatusNotifierFactory>(new[]
            {
                MefTestHelpers.CreateExport<IStatusBarNotifier>(),
                MefTestHelpers.CreateExport<ILogger>()
            });
        }

        [TestMethod]
        public void Create_NullFilePath_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Create(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");
        }

        [TestMethod]
        public void Create_ValidArguments_Created()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Create("some path");

            result.Should().NotBeNull();
            result.Should().BeOfType<AnalysisStatusNotifier>();
        }

        private AnalysisStatusNotifierFactory CreateTestSubject() =>
            new(Mock.Of<IStatusBarNotifier>(), Mock.Of<ILogger>());
    }
}
