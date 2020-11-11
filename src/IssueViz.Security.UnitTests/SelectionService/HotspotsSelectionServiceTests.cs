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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.SelectionService
{
    [TestClass]
    public class HotspotsSelectionServiceTests
    {
        [TestMethod]
        public void Select_NoSubscribers_NoException()
        {
            var testSubject = new HotspotsSelectionService();

            Action act = () => testSubject.Select(Mock.Of<IAnalysisIssueVisualization>());

            act.Should().NotThrow();
        }

        [TestMethod]
        public void Select_HasSubscribers_SubscribersNotified()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();

            var testSubject = new HotspotsSelectionService();
            testSubject.SelectionChanged += eventHandler.Object;

            var hotspot = Mock.Of<IAnalysisIssueVisualization>();
            testSubject.Select(hotspot);

            eventHandler.Verify(
                x => x(testSubject,
                    It.Is((SelectionChangedEventArgs args) => args.SelectedHotspot == hotspot)),
                Times.Once());
        }

        [TestMethod]
        public void Dispose_HasSubscribers_RemovesSubscribers()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();

            var testSubject = new HotspotsSelectionService();
            testSubject.SelectionChanged += eventHandler.Object;

            testSubject.Dispose();

            testSubject.Select(Mock.Of<IAnalysisIssueVisualization>());

            eventHandler.VerifyNoOtherCalls();
        }
    }
}
