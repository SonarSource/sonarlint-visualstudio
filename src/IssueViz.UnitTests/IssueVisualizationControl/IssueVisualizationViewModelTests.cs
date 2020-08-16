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
        private Mock<PropertyChangedEventHandler> propertyChangedEventHandler;

        private IssueVisualizationViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            selectionEventsMock = new Mock<IAnalysisIssueSelectionService>();
            imageServiceMock = new Mock<IVsImageService2>();
            helpLinkProviderMock = new Mock<IRuleHelpLinkProvider>();
            propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();

            testSubject = new IssueVisualizationViewModel(selectionEventsMock.Object, imageServiceMock.Object, helpLinkProviderMock.Object);
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;
        }

        #region Description

        [TestMethod]
        public void Description_CurrentIssueIsNull_Null()
        {
            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueHasNoAnalysisIssue_Null()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueHasAnalysisIssue_IssueMessage()
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
        public void RuleKey_CurrentIssueIsNull_Null()
        {
            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueHasNoAnalysisIssue_Null()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueHasAnalysisIssue_IssueRuleKey()
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
        public void RuleHelpLink_CurrentIssueIsNull_Null()
        {
            testSubject.RuleHelpLink.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleHelpLink_CurrentIssueHasNoAnalysisIssue_Null()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.RuleHelpLink.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleHelpLink_CurrentIssueHasAnalysisIssue_HelpLinkFromLinkProvider()
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
        public void Severity_CurrentIssueIsNull_DefaultSeverity()
        {
            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueHasNoAnalysisIssue_DefaultSeverity()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(selectedIssue));

            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueHasAnalysisIssue_IssueSeverity()
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
        [DataRow(true)]
        [DataRow(false)]
        public void SelectionService_OnIssueChanged_CurrentIssueSetToNewValue(bool isNewIssueNull)
        {
            var selectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            RaiseIssueChangedEvent(selectedIssue);

            testSubject.CurrentIssue.Should().Be(selectedIssue);

            VerifyNotifyPropertyChanged(new string[] {null});
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnIssueChanged_SelectionServiceNotCalled()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            RaiseIssueChangedEvent(selectedIssue);

            selectionEventsMock.VerifyNoOtherCalls();
        }

        #endregion

        #region Flow changed

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectionService_OnFlowChanged_CurrentFlowSetToNewValue(bool isNewFlowNull)
        {
            IAnalysisIssueFlowVisualization selectedFlow = null;

            if (!isNewFlowNull)
            {
                var selectedFlowMock = new Mock<IAnalysisIssueFlowVisualization>();
                selectedFlowMock.Setup(x => x.Locations).Returns(Array.Empty<IAnalysisIssueLocationVisualization>());
                selectedFlow = selectedFlowMock.Object;
            }

            RaiseFlowChangedEvent(selectedFlow);

            testSubject.CurrentFlow.Should().Be(selectedFlow);

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentFlow), nameof(testSubject.LocationListItems));
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnFlowChanged_SelectionServiceNotCalled()
        {
            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(Array.Empty<IAnalysisIssueLocationVisualization>());

            RaiseFlowChangedEvent(selectedFlow.Object);

            selectionEventsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetCurrentFlow_SelectionServiceCalled(bool isNewFlowNull)
        {
            selectionEventsMock.VerifySet(x => x.SelectedFlow = It.IsAny<IAnalysisIssueFlowVisualization>(), Times.Never());

            var selectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.CurrentFlow = selectedFlow;

            selectionEventsMock.VerifySet(x => x.SelectedFlow = selectedFlow, Times.Once());

            selectionEventsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Updating the property will call selectionService, which will raise an event that leads to NotifyPropertyChanged")]
        public void SetCurrentFlow_DoesNotCallNotifyPropertyChanged()
        {
            var selectedFlow = Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.CurrentFlow = selectedFlow;

            propertyChangedEventHandler.VerifyNoOtherCalls();
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

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(locations);

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

            RaiseFlowChangedEvent(selectedFlow.Object);

            testSubject.LocationListItems.Should().BeEquivalentTo(expectedLocationsList, assertionOptions =>
                assertionOptions
                    .WithStrictOrdering()
                    .RespectingRuntimeTypes()
                    .ComparingByMembers<ImageMoniker>());
        }

        #endregion

        #region Location changed

        [TestMethod]
        public void SelectionService_OnLocationChanged_NoCurrentFlow_CurrentLocationSetToNull()
        {
            testSubject.CurrentFlow.Should().BeNull();

            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            RaiseLocationChangedEvent(location);

            testSubject.CurrentLocationListItem.Should().BeNull();
            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_NewLocationIsNull_CurrentLocationSetToNull()
        {
            RaiseLocationChangedEvent(null);

            testSubject.CurrentLocationListItem.Should().BeNull();
            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_LocationIsNotInCurrentFlow_CurrentLocationSetToNull()
        {
            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(Array.Empty<IAnalysisIssueLocationVisualization>());
            RaiseFlowChangedEvent(selectedFlow.Object);
            propertyChangedEventHandler.Reset();

            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            RaiseLocationChangedEvent(location);

            testSubject.CurrentLocationListItem.Should().BeNull();
            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_LocationIsInCurrentFlow_CurrentLocationIsSetToNewValue()
        {
            var location = CreateMockLocation("c:\\test.cpp", KnownMonikers.Test);

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(new []{ location });
            RaiseFlowChangedEvent(selectedFlow.Object);
            propertyChangedEventHandler.Reset();

            RaiseLocationChangedEvent(location);

            testSubject.CurrentLocationListItem.Should().BeEquivalentTo(new LocationListItem(location));
            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnLocationChanged_SelectionServiceNotCalled()
        {
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            RaiseLocationChangedEvent(location);

            selectionEventsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetCurrentLocationListItem_SelectionServiceCalled(bool isCurrentLocationNull)
        {
            selectionEventsMock.VerifySet(x => x.SelectedLocation = It.IsAny<IAnalysisIssueLocationVisualization>(), Times.Never);

            var location = isCurrentLocationNull ? null : Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.CurrentLocationListItem = new LocationListItem(location);

            selectionEventsMock.VerifySet(x => x.SelectedLocation = location, Times.Once);
            selectionEventsMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Updating the property will call selectionService, which will raise an event that leads to NotifyPropertyChanged")]
        public void SetCurrentLocationListItem_DoesNotCallNotifyPropertyChanged()
        {
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.CurrentLocationListItem = new LocationListItem(location);

            propertyChangedEventHandler.VerifyNoOtherCalls();
        }

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

        private void RaiseIssueChangedEvent(IAnalysisIssueVisualization issue)
        {
            selectionEventsMock.Raise(x => x.SelectedIssueChanged += null, new IssueChangedEventArgs(issue));
        }

        private void RaiseFlowChangedEvent(IAnalysisIssueFlowVisualization flow)
        {
            selectionEventsMock.Raise(x => x.SelectedFlowChanged += null, new FlowChangedEventArgs(flow));
        }

        private void RaiseLocationChangedEvent(IAnalysisIssueLocationVisualization location)
        {
            selectionEventsMock.Raise(x => x.SelectedLocationChanged += null, new LocationChangedEventArgs(location));
        }

        private void VerifyNotifyPropertyChanged(params string[] changedProperties)
        {
            foreach (var changedProperty in changedProperties)
            {
                propertyChangedEventHandler.Verify(x =>
                        x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == changedProperty)),
                    Times.Once);
            }

            propertyChangedEventHandler.VerifyNoOtherCalls();
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
    }
}
