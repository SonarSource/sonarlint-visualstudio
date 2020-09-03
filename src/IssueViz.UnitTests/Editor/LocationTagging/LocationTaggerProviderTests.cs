/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.ErrorTagging;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class LocationTaggerProviderTests : CommonTaggerProviderTestsBase
    {
        private readonly IIssueLocationStore ValidLocationStore = Mock.Of<IIssueLocationStore>();
        private readonly IIssueSpanCalculator ValidSpanCalculator = Mock.Of<IIssueSpanCalculator>();
        private readonly ILogger ValidLogger = Mock.Of<ILogger>();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var storeExport = MefTestHelpers.CreateExport<IIssueLocationStore>(ValidLocationStore);
            var calculatorExport = MefTestHelpers.CreateExport<IIssueSpanCalculator>(ValidSpanCalculator);
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(ValidLogger);

            MefTestHelpers.CheckTypeCanBeImported<LocationTaggerProvider, ITaggerProvider>(null, new[] { storeExport, calculatorExport, loggerExport});
        }

        protected override ITaggerProvider CreateTestSubject() =>
            (ITaggerProvider) new LocationTaggerProvider(ValidLocationStore, ValidSpanCalculator, ValidLogger);
    }
}
