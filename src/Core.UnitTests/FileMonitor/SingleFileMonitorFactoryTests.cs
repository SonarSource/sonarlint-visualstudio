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

using System.IO;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.FileMonitor;

[TestClass]
public class SingleFileMonitorFactoryTests
{
    private SingleFileMonitorFactory testSubject;
    private ILogger logger;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.For<ILogger>();
        testSubject = new SingleFileMonitorFactory(logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SingleFileMonitorFactory, ISingleFileMonitorFactory>(MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void Create_NullFilePathToMonitor_ArgumentNullException()
    {
        string filePathToMonitor = null;

        Action act = () => testSubject.Create(filePathToMonitor);

        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be(nameof(filePathToMonitor));
    }

    [TestMethod]
    public void Create_ValidArguments_ReturnsInstance()
    {
        var filePathToMonitor = Path.Combine(TestContext.TestDir, $"{nameof(SingleFileMonitorFactoryTests)}.cs");

        var singleFileMonitor =  testSubject.Create(filePathToMonitor);

        singleFileMonitor.Should().NotBeNull();
        singleFileMonitor.MonitoredFilePath.Should().Be(filePathToMonitor);
    }
}
