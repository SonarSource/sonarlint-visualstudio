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
using System.Collections.Generic;
using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using ImageMoniker = Microsoft.VisualStudio.Imaging.Interop.ImageMoniker;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class IssueVisualizationViewModelTests
    {
        private const AnalysisIssueSeverity DefaultNullIssueSeverity = AnalysisIssueSeverity.Info;

        private Mock<IAnalysisIssueSelectionService> selectionEventsMock;
        private Mock<IVsImageService2> imageServiceMock;
        private Mock<IRuleHelpLinkProvider> helpLinkProviderMock;

        private IssueVisualizationViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            selectionEventsMock = new Mock<IAnalysisIssueSelectionService>();
            imageServiceMock = new Mock<IVsImageService2>();
            helpLinkProviderMock = new Mock<IRuleHelpLinkProvider>();
            testSubject = new IssueVisualizationViewModel(selectionEventsMock.Object, imageServiceMock.Object, helpLinkProviderMock.Object);
        }

        #region Description

        [TestMethod]
        public void Description_NoCurrentIssueVisualization_Null()
        {
            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueVisualizationHasNoAnalysisIssue_Null()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueVisualizationHasAnalysisIssue_IssueMessage()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Message).Returns("test message");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(issueViz.Object));

            testSubject.Description.Should().Be("test message");
        }

        #endregion

        #region RuleKey

        [TestMethod]
        public void RuleKey_NoCurrentIssueVisualization_Null()
        {
            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueVisualizationHasNoAnalysisIssue_Null()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueVisualizationHasAnalysisIssue_IssueRuleKey()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test RuleKey");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(issueViz.Object));

            testSubject.RuleKey.Should().Be("test RuleKey");
        }

        #endregion

        #region RuleHelpLink

        [TestMethod]
        public void RuleHelpLink_NoCurrentIssueVisualization_Null()
        {
            testSubject.RuleHelpLink.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleHelpLink_CurrentIssueVisualizationHasNoAnalysisIssue_Null()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.RuleHelpLink.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleHelpLink_CurrentIssueVisualizationHasAnalysisIssue_RuleHelpLinkFromLinkProvider()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test RuleKey");

            helpLinkProviderMock.Setup(x => x.GetHelpLink("test RuleKey")).Returns("test link");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(issueViz.Object));

            testSubject.RuleHelpLink.Should().Be("test link");
        }

        #endregion

        #region Severity

        [TestMethod]
        public void Severity_NoCurrentIssueVisualization_DefaultSeverity()
        {
            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueVisualizationHasNoAnalysisIssue_DefaultSeverity()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueVisualizationHasAnalysisIssue_IssueSeverity()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Severity).Returns(AnalysisIssueSeverity.Blocker);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(issueViz.Object));

            testSubject.Severity.Should().Be(AnalysisIssueSeverity.Blocker);
        }

        #endregion

        #region Issue changed

        [TestMethod]
        public void SelectionService_OnIssueChanged_CallsNotifyPropertyChangedWithNullProperty()
        {
            testSubject.CurrentIssue.Should().BeNull();

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.CurrentIssue.Should().Be(selectedIssue);

            eventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == null)),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnIssueChanged_SelectionServiceNotCalledAgain()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            selectionEventsMock.VerifyNoOtherCalls();
        }

        #endregion

        #region Flow changed

        [TestMethod]
        public void SelectionService_OnFlowChanged_CallsNotifyPropertyChanged()
        {
            testSubject.CurrentFlow.Should().BeNull();

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x=> x.Locations).Returns(Array.Empty<IAnalysisIssueLocationVisualization>());

            selectionEventsMock.Raise(x => x.SelectedFlowChanged += null, new FlowChangedEventArgs(selectedFlow.Object));

            testSubject.CurrentFlow.Should().Be(selectedFlow.Object);

            eventHandler.Verify(x=> 
                x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == nameof(testSubject.CurrentFlow))), 
                Times.Once);

            eventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == nameof(testSubject.LocationListItems))),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnFlowChanged_SelectionServiceNotCalled()
        {
            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(Array.Empty<IAnalysisIssueLocationVisualization>());

            selectionEventsMock.Raise(x => x.SelectedFlowChanged += null, new FlowChangedEventArgs(selectedFlow.Object));

            selectionEventsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CurrentFlow_OnFlowChanged_SelectionServiceCalled()
        {
            selectionEventsMock.VerifySet(x => x.SelectedFlow = It.IsAny<IAnalysisIssueFlowVisualization>(), Times.Never());

            var selectedFlow = Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.CurrentFlow = selectedFlow;

            selectionEventsMock.VerifySet(x => x.SelectedFlow = selectedFlow, Times.Once());

            selectionEventsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Updating the property will call selectionService, which will raise an event that leads to NotifyPropertyChanged")]
        public void CurrentFlow_OnFlowChanged_DoesNotCallNotifyPropertyChanged()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            var selectedFlow = Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.CurrentFlow = selectedFlow;

            eventHandler.VerifyNoOtherCalls();
        }

        #endregion

        #region Locations List

        [TestMethod]
        public void SelectionService_OnFlowChanged_LocationsListUpdated()
        {
            var locations = new List<IAnalysisIssueLocationVisualization>
            {
                CreateMockLocation("c:\\test\\c1.c", KnownMonikers.CFile),
                CreateMockLocation("c:\\c1.c", KnownMonikers.CFile),
                CreateMockLocation("c:\\test\\c2.cpp", KnownMonikers.CPPFile),
                CreateMockLocation("c:\\test\\c2.cpp", KnownMonikers.CPPFile),
                CreateMockLocation("c:\\c3.h", KnownMonikers.CPPHeaderFile),
                CreateMockLocation("c:\\c3.h", KnownMonikers.CPPHeaderFile),
                CreateMockLocation("c:\\test\\c1.c", KnownMonikers.CFile)
            };

            var expectedLocationsList = new List<ILocationListItem>
            {
                new FileNameLocationListItem("c:\\test\\c1.c", "c1.c", KnownMonikers.CFile),
                new LocationListItem(locations[0]),
                new FileNameLocationListItem("c:\\c1.c", "c1.c", KnownMonikers.CFile),
                new LocationListItem(locations[1]),
                new FileNameLocationListItem("c:\\test\\c2.cpp", "c2.cpp", KnownMonikers.CPPFile),
                new LocationListItem(locations[2]),
                new LocationListItem(locations[3]),
                new FileNameLocationListItem("c:\\c3.h", "c3.h", KnownMonikers.CPPHeaderFile),
                new LocationListItem(locations[4]),
                new LocationListItem(locations[5]),
                new FileNameLocationListItem("c:\\test\\c1.c", "c1.c", KnownMonikers.CFile),
                new LocationListItem(locations[6]),
            };

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(locations);

            selectionEventsMock.Raise(x => x.SelectedFlowChanged += null, new FlowChangedEventArgs(selectedFlow.Object));

            testSubject.LocationListItems.Should().BeEquivalentTo(expectedLocationsList, assertionOptions =>
                assertionOptions
                    .WithStrictOrdering()
                    .RespectingRuntimeTypes()
                    .ComparingByMembers<ImageMoniker>());
        }

        private IAnalysisIssueLocationVisualization CreateMockLocation(string filePath, object imageMoniker)
        {
            imageServiceMock.Setup(x => x.GetImageMonikerForFile(filePath)).Returns((ImageMoniker)imageMoniker);

            var location = new Mock<IAnalysisIssueLocation>();
            location.Setup(x => x.FilePath).Returns(filePath);

            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns(location.Object);

            return locationViz.Object;
        }

        #endregion

        #region Location changed

        #endregion

        #region Dispose

        [TestMethod]
        public void Dispose_UnregisterFromSelectionServiceEvents()
        {
            selectionEventsMock.SetupRemove(m => m.SelectedIssueChanged -= (sender, args) => { });
            selectionEventsMock.SetupRemove(m => m.SelectedFlowChanged -= (sender, args) => { });
            selectionEventsMock.SetupRemove(m => m.SelectedLocationChanged -= (sender, args) => { });
           
            testSubject.Dispose();

            selectionEventsMock.VerifyRemove(x=> x.SelectedIssueChanged -= It.IsAny<EventHandler<IssueChangedEventArgs>>(), Times.Once);
            selectionEventsMock.VerifyRemove(x=> x.SelectedFlowChanged -= It.IsAny<EventHandler<FlowChangedEventArgs>>(), Times.Once);
            selectionEventsMock.VerifyRemove(x=> x.SelectedLocationChanged -= It.IsAny<EventHandler<LocationChangedEventArgs>>(), Times.Once);
            selectionEventsMock.Verify(x=> x.Dispose(), Times.Once);
        }

        #endregion
    }
}
