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
using SonarLint.VisualStudio.Integration.UnitTests;
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

        private Mock<IAnalysisIssueSelectionService> selectionServiceMock;
        private Mock<IVsImageService2> imageServiceMock;
        private Mock<IRuleHelpLinkProvider> helpLinkProviderMock;
        private TestLogger logger;
        private Mock<PropertyChangedEventHandler> propertyChangedEventHandler;

        private IssueVisualizationViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            imageServiceMock = new Mock<IVsImageService2>();
            helpLinkProviderMock = new Mock<IRuleHelpLinkProvider>();
            logger = new TestLogger();
            propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();

            testSubject = new IssueVisualizationViewModel(selectionServiceMock.Object, imageServiceMock.Object, helpLinkProviderMock.Object, logger);
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
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, Mock.Of<IAnalysisIssueVisualization>());

            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueHasAnalysisIssue_IssueMessage()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Message).Returns("test message");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz.Object);

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
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, Mock.Of<IAnalysisIssueVisualization>());

            testSubject.RuleKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKey_CurrentIssueHasAnalysisIssue_IssueRuleKey()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test RuleKey");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz.Object);

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
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, Mock.Of<IAnalysisIssueVisualization>());

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

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz.Object);

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
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, Mock.Of<IAnalysisIssueVisualization>());

            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueHasAnalysisIssue_IssueSeverity()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Severity).Returns(AnalysisIssueSeverity.Blocker);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz.Object);

            testSubject.Severity.Should().Be(AnalysisIssueSeverity.Blocker);
        }

        #endregion

        #region Issue changed

        [TestMethod]
        public void SelectionService_OnIssueChanged_PreviousIssueIsNull_NewIssueIsNull_NoChanges()
        {
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, null, null, null);

            testSubject.CurrentIssue.Should().BeNull();

            propertyChangedEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_PreviousIssueIsNull_NewIssueIsNotNull_CurrentIssueSetToNewValue()
        {
            var newIssue = Mock.Of<IAnalysisIssueVisualization>();
            var newFlow = CreateFlow(out var newLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, newIssue, newFlow, newLocation);

            testSubject.CurrentIssue.Should().Be(newIssue);

            VerifyNotifyPropertyChanged("", nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_PreviousIssueIsNotNull_NewIssueIsNull_CurrentIssueSetToNull()
        {
            var previousIssue = Mock.Of<IAnalysisIssueVisualization>();
            var previousFlow = CreateFlow(out var previousLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, previousIssue, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, null, null, null);

            testSubject.CurrentIssue.Should().BeNull();

            VerifyNotifyPropertyChanged("", nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_PreviousIssueIsNotNull_NewIssueIsNotNull_CurrentIssueSetToNewValue()
        {
            var previousIssue = Mock.Of<IAnalysisIssueVisualization>();
            var previousFlow = CreateFlow(out var previousLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, previousIssue, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            var newIssue = Mock.Of<IAnalysisIssueVisualization>();
            var newFlow = CreateFlow(out var newLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, newIssue, newFlow, newLocation);

            testSubject.CurrentIssue.Should().Be(newIssue);

            VerifyNotifyPropertyChanged("", nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnIssueChanged_SelectionServiceNotCalled()
        {
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, Mock.Of<IAnalysisIssueVisualization>());

            selectionServiceMock.VerifyNoOtherCalls();
        }

        #endregion

        #region Flow changed

        [TestMethod]
        public void SelectionService_OnFlowChanged_PreviousFlowIsNull_NewFlowIsNull_NoChanges()
        {
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, null);

            testSubject.CurrentFlow.Should().BeNull();

            propertyChangedEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_PreviousFlowIsNotNull_NewFlowIsNull_CurrentFlowSetToNull()
        {
            var previousFlow = CreateFlow(out var previousLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, null);

            testSubject.CurrentFlow.Should().BeNull();

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentFlow), nameof(testSubject.LocationListItems), nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_PreviousFlowIsNull_NewFlowIsNotNull_CurrentFlowSetToNull()
        {
            var newFlow = CreateFlow(out var newLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, newFlow, newLocation);

            testSubject.CurrentFlow.Should().Be(newFlow);

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentFlow), nameof(testSubject.LocationListItems), nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_PreviousFlowIsNotNull_NewFlowIsNotNull_CurrentFlowSetToNewValue()
        {
            var previousFlow = CreateFlow(out var previousLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            var newFlow = CreateFlow(out var newLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, newFlow, newLocation);

            testSubject.CurrentFlow.Should().Be(newFlow);

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentFlow), nameof(testSubject.LocationListItems), nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnFlowChanged_SelectionServiceNotCalled()
        {
            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(Array.Empty<IAnalysisIssueLocationVisualization>());

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object);

            selectionServiceMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetCurrentFlow_SelectionServiceCalled(bool isNewFlowNull)
        {
            selectionServiceMock.Verify(x => x.Select(It.IsAny<IAnalysisIssueFlowVisualization>()), Times.Never());

            var selectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.CurrentFlow = selectedFlow;

            selectionServiceMock.Verify(x => x.Select(selectedFlow), Times.Once());

            selectionServiceMock.VerifyNoOtherCalls();
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
        public void SelectionService_OnFlowChanged_FlowWithOneLocation_LocationsListUpdated()
        {
            var location = CreateMockLocation("c:\\test\\c1.c", KnownMonikers.CFile);

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(new[] { location });

            var expectedLocationsList = new List<ILocationListItem>
            {
                new FileNameLocationListItem("c:\\test\\c1.c", "c1.c", KnownMonikers.CFile),
                new LocationListItem(location)
            };

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object);

            VerifyLocationList(expectedLocationsList);
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_FlowWithMultipleLocations_LocationsListUpdated()
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

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object);

            VerifyLocationList(expectedLocationsList);
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_FailsToRetrieveFileIcon_BlankIcon()
        {
            var location = CreateMockLocation("c:\\test\\c1.c", KnownMonikers.CFile, new NotImplementedException("this is a test"));

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(new[] { location });

            var expectedIcon = KnownMonikers.Blank;

            var expectedLocationsList = new List<ILocationListItem>
            {
                new FileNameLocationListItem("c:\\test\\c1.c", "c1.c", expectedIcon),
                new LocationListItem(location)
            };

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object);

            VerifyLocationList(expectedLocationsList);

            logger.AssertPartialOutputStringExists("this is a test");
            logger.OutputStrings.Count.Should().Be(1);
        }

        #endregion

        #region Location changed

        [TestMethod]
        public void SelectionService_OnLocationChanged_PreviousLocationIsNull_NewLocationIsNull_NoChanges()
        {
            RaiseSelectionChangedEvent(SelectionChangeLevel.Location, null, null, null);

            testSubject.CurrentLocationListItem.Should().BeNull();

            propertyChangedEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_PreviousLocationNotIsNull_NewLocationIsNull_CurrentLocationSetToNull()
        {
            var selectedFlow = CreateFlow(out var previousLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Location, null, selectedFlow, null);

            testSubject.CurrentLocationListItem.Should().BeNull();

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_LocationIsNotInCurrentFlow_CurrentLocationSetToNull()
        {
            var selectedFlow = CreateFlow(out var previousLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Location, null, selectedFlow, Mock.Of<IAnalysisIssueLocationVisualization>());

            testSubject.CurrentLocationListItem.Should().BeNull();

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_LocationIsInCurrentFlow_CurrentLocationIsSetToNewValue()
        {
            var locations = new List<IAnalysisIssueLocationVisualization>
            {
                CreateMockLocation("c:\\test\\c1.c", KnownMonikers.CFile),
                CreateMockLocation("c:\\c1.c", KnownMonikers.CFile),
            };

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(locations);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object, locations[0]);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Location, null, selectedFlow.Object, locations[1]);

            testSubject.CurrentLocationListItem.Should().BeEquivalentTo(new LocationListItem(locations[1]));

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnLocationChanged_SelectionServiceNotCalled()
        {
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Location, null, null, location);

            selectionServiceMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetCurrentLocationListItem_SelectionServiceCalled()
        {
            selectionServiceMock.Verify(x => x.Select(It.IsAny<IAnalysisIssueLocationVisualization>()), Times.Never());

            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.CurrentLocationListItem = new LocationListItem(location);

            selectionServiceMock.Verify(x => x.Select(location), Times.Once());

            selectionServiceMock.VerifyNoOtherCalls();
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
            selectionServiceMock.SetupRemove(m => m.SelectionChanged -= (sender, args) => { });

            testSubject.Dispose();

            selectionServiceMock.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
            selectionServiceMock.VerifyNoOtherCalls();
        }

        #endregion

        private void RaiseSelectionChangedEvent(SelectionChangeLevel changeLevel,
            IAnalysisIssueVisualization issue = null,
            IAnalysisIssueFlowVisualization flow = null,
            IAnalysisIssueLocationVisualization location = null)
        {
            selectionServiceMock.Raise(x => x.SelectionChanged += null,
                new SelectionChangedEventArgs(changeLevel, issue, flow, location));
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

        private IAnalysisIssueFlowVisualization CreateFlow(out IAnalysisIssueLocationVisualization location)
        {
            location = CreateMockLocation("c:\\test.cpp", KnownMonikers.Test);

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(new[] { location });

            return selectedFlow.Object;
        }

        private IAnalysisIssueLocationVisualization CreateMockLocation(string filePath, object imageMoniker, Exception failsToRetrieveMoniker = null)
        {
            if (failsToRetrieveMoniker != null)
            {
                imageServiceMock
                    .Setup(x => x.GetImageMonikerForFile(filePath))
                    .Throws(failsToRetrieveMoniker);
            }
            else
            {
                imageServiceMock.Setup(x => x.GetImageMonikerForFile(filePath)).Returns((ImageMoniker)imageMoniker);
            }

            var location = new Mock<IAnalysisIssueLocation>();
            location.Setup(x => x.FilePath).Returns(filePath);

            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns(location.Object);

            return locationViz.Object;
        }

        private void VerifyLocationList(IEnumerable<ILocationListItem> expectedLocationsList)
        {
            testSubject.LocationListItems.Should().BeEquivalentTo(expectedLocationsList, assertionOptions =>
                assertionOptions
                    .WithStrictOrdering()
                    .RespectingRuntimeTypes() // check underlying types rather than ILocationListItem
                    .ComparingByMembers<ImageMoniker>()); // check struct fields and properties
        }
    }
}
