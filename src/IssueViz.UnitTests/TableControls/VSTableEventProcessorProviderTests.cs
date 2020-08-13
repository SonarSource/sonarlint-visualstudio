/*
 * SonarQube Client
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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.TableControls
{
    [TestClass]
    public class VSTableEventProcessorProviderTests
    {
        [TestMethod]
        public void GetProcessor_NullTableControl_ReturnsNull()
        {
            var testSubject = (ITableControlEventProcessorProvider)new VSTableEventProcessorProvider(Mock.Of<IIssueTablesSelectionMonitor>());

            testSubject.GetAssociatedEventProcessor(null)
                .Should().BeNull();
        }

        [TestMethod]
        public void GetProcessor_ReturnsNewProcessorInstanceForEachCall()
        {
            var mockTable = Mock.Of<IWpfTableControl>();
            var monitorMock = new Mock<IIssueTablesSelectionMonitor>();
            var testSubject = (ITableControlEventProcessorProvider)new VSTableEventProcessorProvider(monitorMock.Object);

            // First call
            var processor1 = testSubject.GetAssociatedEventProcessor(mockTable);
            processor1.Should().NotBeNull();

            // Second call
            var processor2 = testSubject.GetAssociatedEventProcessor(mockTable);
            processor2.Should().NotBeNull();

            processor1.Should().NotBeSameAs(processor2);
        }
    }
}
