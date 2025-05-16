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

using System.ComponentModel;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList
{
    [TestClass]
    public class HotspotViewModelTests
    {
        private IAnalysisIssueVisualization issueViz;
        private ISecurityCategoryDisplayNameProvider securityCategoryDisplayNameProvider;
        private IIssueVizDisplayPositionCalculator positionCalculator;
        private HotspotViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            issueViz = Substitute.For<IAnalysisIssueVisualization>();
            securityCategoryDisplayNameProvider = Substitute.For<ISecurityCategoryDisplayNameProvider>();
            positionCalculator = Substitute.For<IIssueVizDisplayPositionCalculator>();
            testSubject = new HotspotViewModel(issueViz, default, default, securityCategoryDisplayNameProvider, positionCalculator);
        }

        [TestMethod]
        public void Ctor_RegisterToHotspotPropertyChangedEvent() => issueViz.Received(1).PropertyChanged += Arg.Any<PropertyChangedEventHandler>();

        [TestMethod]
        public void Dispose_UnregisterFromHotspotPropertyChangedEvent()
        {
            testSubject.Dispose();

            issueViz.Received(1).PropertyChanged -= Arg.Any<PropertyChangedEventHandler>();
        }

        [TestMethod]
        public void HotspotPropertyChanged_UnknownProperty_NoPropertyChangedEvent()
        {
            var eventHandler = Substitute.For<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler;
            eventHandler.ReceivedCalls().Should().BeEmpty();

            issueViz.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(null, new PropertyChangedEventArgs("some dummy property"));

            eventHandler.DidNotReceive().Invoke(testSubject, Arg.Any<PropertyChangedEventArgs>());
        }

        [TestMethod]
        public void HotspotPropertyChanged_SpanProperty_RaisesPropertyChangedForLineAndColumn()
        {
            var eventHandler = Substitute.For<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler;
            eventHandler.ReceivedCalls().Should().BeEmpty();

            issueViz.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.Span)));

            eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(IHotspotViewModel.Line)));
            eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(IHotspotViewModel.Column)));
        }

        [TestMethod]
        public void Line_ReturnsDisplayPosition()
        {
            positionCalculator.GetLine(issueViz).Returns(123);

            testSubject.Line.Should().Be(123);
            positionCalculator.Received(1).GetLine(issueViz);
        }

        [TestMethod]
        public void Column_ReturnsDisplayPosition()
        {
            positionCalculator.GetColumn(issueViz).Returns(999);

            testSubject.Column.Should().Be(999);
            positionCalculator.Received(1).GetColumn(issueViz);
        }

        [TestMethod]
        public void DisplayPath_HotspotHasNoFilePath_ReturnsServerPath()
        {
            var hotspot = MockHotspot();
            hotspot.ServerFilePath.Returns("\\some\\server\\path.cs");
            issueViz.CurrentFilePath.Returns((string)null);

            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void DisplayPath_HotspotHasFilePath_ReturnsFilePath()
        {
            MockHotspot();
            issueViz.CurrentFilePath.Returns("c:\\some\\local\\path.cs");

            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void CategoryDisplayName_ReturnsFriendlyName()
        {
            var hotspot = MockHotspot();
            hotspot.Rule.SecurityCategory.Returns("some category");
            securityCategoryDisplayNameProvider.Get("some category").Returns("some display name");

            testSubject.CategoryDisplayName.Should().Be("some display name");
        }

        [TestMethod]
        public void HotspotPropertyChanged_CurrentFilePathProperty_RaisesPropertyChangedForDisplayPath()
        {
            var eventHandler = Substitute.For<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler;
            eventHandler.ReceivedCalls().Should().BeEmpty();

            issueViz.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.CurrentFilePath)));

            eventHandler.Received(1).Invoke(testSubject, Arg.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(IHotspotViewModel.DisplayPath)));
            eventHandler.ReceivedCalls().Count().Should().Be(1);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("serverKey")]
        public void ExistsOnServer_ReturnsTrueIfServerKeyIsNotNull(string serverKey)
        {
            var analysisBase = Substitute.For<IAnalysisIssueBase>();
            issueViz.Issue.Returns(analysisBase);
            issueViz.Issue.IssueServerKey.Returns(serverKey);

            testSubject.ExistsOnServer.Should().Be(serverKey != null);
        }

        private IHotspot MockHotspot()
        {
            var hotspot = Substitute.For<IHotspot>();
            issueViz.Issue.Returns(hotspot);
            return hotspot;
        }
    }
}
