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

using System;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Migration;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class FixedStepsProgressToMigrationProgressAdapterTests
    {
        [TestMethod]
        public void Report_ReportsMigrationProgress()
        {
            var migrationProgress = new Mock<IProgress<MigrationProgress>>();
            MigrationProgress migrationProgressReport = null;
            migrationProgress.Setup(x => x.Report(It.IsAny<MigrationProgress>())).Callback<MigrationProgress>(x => migrationProgressReport = x) ;

            var testSubject = new FixedStepsProgressToMigrationProgressAdapter(migrationProgress.Object);
            testSubject.Report(new FixedStepsProgress("test message", 0, 1));

            migrationProgress.Verify(x => x.Report(migrationProgressReport), Times.Once);
            migrationProgressReport.Message.Should().Be("test message");
        }
    }
}
