/*
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

using SonarLint.VisualStudio.Core.Logging;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class SonarLintOutputLoggerFactory
    {
        // the normal check for export does not work here because this is a special Property export instead of the normal Class export
        // [TestMethod]
        // public void MefCtor_CheckIsExported()
        // {
        //     MefTestHelpers.CheckTypeCanBeImported<SonarLintOutputLoggerFactory, ILogger>(
        //         MefTestHelpers.CreateExport<ILoggerFactory>(),
        //         MefTestHelpers.CreateExport<SVsServiceProvider>(),
        //         MefTestHelpers.CreateExport<ISonarLintSettings>());
        // }

        [TestMethod]
        public void Ctor_CreatesLoggerWithExpectedParameters()
        {
            var loggerFactory = Substitute.For<ILoggerFactory>();

            var testSubject = new Integration.Helpers.SonarLintOutputLoggerFactory(loggerFactory, Substitute.For<IServiceProvider>(), Substitute.For<ISonarLintSettings>());

            testSubject.Instance.Should().NotBeNull();
            loggerFactory.Create(Arg.Any<SonarLintOutputWindowLoggerWriter>(), Arg.Any<SonarLintSettingsLoggerSettingsProvider>());
        }

    }
}
