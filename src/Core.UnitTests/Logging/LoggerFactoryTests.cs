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
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Logging;

[TestClass]
public class LoggerFactoryTests
{
    private ILoggerContextManager logContextManager;
    private ILoggerWriter logWriter;
    private ILoggerSettingsProvider logVerbosityIndicator;
    private LoggerFactory testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        logContextManager = Substitute.For<ILoggerContextManager>();
        logWriter = Substitute.For<ILoggerWriter>();
        logVerbosityIndicator = Substitute.For<ILoggerSettingsProvider>();
        testSubject = new LoggerFactory(logContextManager);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<LoggerFactory, ILoggerFactory>(
            MefTestHelpers.CreateExport<ILoggerContextManager>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsNonSharedMefComponent<LoggerFactory>();

    [TestMethod]
    public void Create_ReturnsLoggerConfiguredWithCorrectDependencies()
    {
        var logger = testSubject.Create(logWriter, logVerbosityIndicator);

        logger.Should().NotBeNull();
        logger.WriteLine("msg");
        logContextManager.Received().GetFormattedContextOrNull(default);
        _ = logVerbosityIndicator.Received().IsVerboseEnabled;
        logWriter.Received().WriteLine(Arg.Is<string>(x => x.Contains("msg")));
    }
}
