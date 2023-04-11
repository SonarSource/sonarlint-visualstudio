/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.Common;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class IssueVisualizationViewModelTests
    {
        private const AnalysisIssueSeverity DefaultNullIssueSeverity = AnalysisIssueSeverity.Info;

        private Mock<IAnalysisIssueSelectionService> selectionServiceMock;
        private Mock<ILocationNavigator> locationNavigatorMock;
        private Mock<IFileNameLocationListItemCreator> fileNameLocationListItemCreatorMock;

        private Mock<PropertyChangedEventHandler> propertyChangedEventHandler;

        private IssueVisualizationViewModel testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            locationNavigatorMock = new Mock<ILocationNavigator>();
            fileNameLocationListItemCreatorMock = new Mock<IFileNameLocationListItemCreator>();
            propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();

            testSubject = CreateTestSubject();

            selectionServiceMock.Invocations.Clear();
        }

        private IssueVisualizationViewModel CreateTestSubject()
        {
            var viewModel = new IssueVisualizationViewModel(selectionServiceMock.Object,
                locationNavigatorMock.Object,
                fileNameLocationListItemCreatorMock.Object,
                Mock.Of<INavigateToCodeLocationCommand>(),
                Mock.Of<INavigateToRuleDescriptionCommand>(),
                Mock.Of<INavigateToDocumentationCommand>());

            viewModel.PropertyChanged += propertyChangedEventHandler.Object;

            return viewModel;
        }

        #region Initialization

        [TestMethod]
        public void Ctor_InitializeWithExistingSelection()
        {
            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);
            var newIssue = CreateIssue(flows: new[] { newFlow });

            selectionServiceMock.Setup(x => x.SelectedIssue).Returns(newIssue);
            selectionServiceMock.Setup(x => x.SelectedFlow).Returns(newFlow);
            selectionServiceMock.Setup(x => x.SelectedLocation).Returns(newLocation);

            testSubject = CreateTestSubject();

            testSubject.CurrentIssue.Should().Be(newIssue);
            testSubject.CurrentFlow.Should().Be(newFlow);
            testSubject.CurrentLocationListItem.Should().NotBeNull();
            testSubject.CurrentLocationListItem.Location.Should().Be(newLocation);
        }

        #endregion

        #region CurrentIssue PropertyChanged

        [TestMethod]
        public void CurrentIssuePropertyChanged_CurrentFilePathProperty_RaisesNotifyPropertyChanged()
        {
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);
            propertyChangedEventHandler.Reset();

            RaisePropertyChangedEvent(Mock.Get(issueViz), nameof(IAnalysisIssueVisualization.CurrentFilePath));

            VerifyNotifyPropertyChanged(nameof(IIssueVisualizationViewModel.FileName));
        }

        [TestMethod]
        public void CurrentIssuePropertyChanged_Span_RaisesNotifyPropertyChanged()
        {
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);
            propertyChangedEventHandler.Reset();

            RaisePropertyChangedEvent(Mock.Get(issueViz), nameof(IAnalysisIssueVisualization.Span));

            VerifyNotifyPropertyChanged(nameof(IIssueVisualizationViewModel.LineNumber), nameof(IIssueVisualizationViewModel.HasNonNavigableLocations));
        }

        [TestMethod]
        public void CurrentIssuePropertyChanged_UnknownProperty_NotifyPropertyChangedNotRaised()
        {
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);
            propertyChangedEventHandler.Reset();

            RaisePropertyChangedEvent(Mock.Get(issueViz), "some unknown property");

            VerifyNotifyPropertyChangedNotRaised();
        }

        #endregion

        #region Issue Locations PropertyChanged

        [TestMethod]
        public void CurrentIssueLocationsPropertyChanged_SpanProperty_RaisesNotifyPropertyChanged()
        {
            var location = CreateLocation();
            var flow = CreateFlow(location);
            var issue = CreateIssue(flows: new[] { flow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issue, flow, location);

            propertyChangedEventHandler.Reset();

            RaisePropertyChangedEvent(Mock.Get(location), nameof(IAnalysisIssueLocationVisualization.Span));

            VerifyNotifyPropertyChanged(nameof(IIssueVisualizationViewModel.HasNonNavigableLocations));
        }

        [TestMethod]
        public void CurrentIssueLocationsPropertyChanged_UnknownProperty_NotifyPropertyChangedNotRaised()
        {
            var location = CreateLocation();
            var flow = CreateFlow(location);
            var issue = CreateIssue(flows: new[] { flow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issue, flow, location);
            propertyChangedEventHandler.Reset();

            RaisePropertyChangedEvent(Mock.Get(location), "some unknown property");

            VerifyNotifyPropertyChangedNotRaised();
        }

        #endregion

        #region HasNonNavigableLocations

        [TestMethod]
        public void HasNonNavigableLocations_CurrentIssueIsNull_False()
        {
            testSubject.HasNonNavigableLocations.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, true, true)]
        [DataRow(true, false, true)]
        [DataRow(true, true, true)]
        public void HasNonNavigableLocations_ReturnsTrueIfPrimaryOrSecondaryHaveNonNavigableSpans(bool issueIsNavigable, bool locationIsNavigable, bool expectedNonNavigableLocations)
        {
            var navigableSpan = (SnapshotSpan?)null;
            var locationSpan = locationIsNavigable ? new SnapshotSpan() : navigableSpan;
            var issueSpan = issueIsNavigable ? new SnapshotSpan() : navigableSpan;

            var testedLocation = CreateLocation("test.cpp", locationSpan);
            var navigableLocation = CreateLocation("test.cpp", navigableSpan);

            var flow = CreateFlow(testedLocation, navigableLocation);
            var issue = CreateIssue(span: issueSpan, flows: new[] { flow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issue);

            testSubject.HasNonNavigableLocations.Should().Be(expectedNonNavigableLocations);
        }

        #endregion

        #region LineNumber

        [TestMethod]
        public void LineNumber_CurrentIssueIsNull_Null()
        {
            testSubject.LineNumber.Should().BeNull();
        }

        [TestMethod]
        public void LineNumber_CurrentIssueHasNoSpan_Null()
        {
            var issueViz = CreateIssue(span: (SnapshotSpan?)null);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.LineNumber.Should().BeNull();
        }

        [TestMethod]
        public void LineNumber_CurrentIssueHasEmptySpan_Null()
        {
            var issueViz = CreateIssue(span: new SnapshotSpan());

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.LineNumber.Should().BeNull();
        }

        [TestMethod]
        public void LineNumber_CurrentIssueHasValidSpan_OneBasedLineNumber()
        {
            const int zeroBasedLineNumber = 10;
            var textLine = new Mock<ITextSnapshotLine>();
            textLine.Setup(x => x.LineNumber).Returns(zeroBasedLineNumber);

            const int mockPosition = 5;
            var textSnapshot = Mock.Get(TaggerTestHelper.CreateSnapshot());
            textSnapshot.Setup(x => x.GetLineFromPosition(mockPosition)).Returns(textLine.Object);

            var snapshotSpan = new SnapshotSpan(textSnapshot.Object, new Span(mockPosition, 1));

            var issueViz = CreateIssue(span: snapshotSpan);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            var expectedOneBasedLineNumber = zeroBasedLineNumber + 1;
            testSubject.LineNumber.Should().Be(expectedOneBasedLineNumber);
        }

        #endregion

        #region FileName

        [TestMethod]
        public void FileName_CurrentIssueIsNull_Null()
        {
            testSubject.FileName.Should().BeNull();
        }

        [TestMethod]
        public void FileName_CurrentIssueIsNotNull_FileNameIsTakenFromCurrentFilePath()
        {
            var issueViz = CreateIssue(filePath: "c:\\a\\b\\test.cpp");

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.FileName.Should().Be("test.cpp");
        }

        #endregion

        #region Description

        [TestMethod]
        public void Description_CurrentIssueIsNull_Null()
        {
            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueHasNoAnalysisIssue_Null()
        {
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.Description.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Description_CurrentIssueHasAnalysisIssue_IssueMessage()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.PrimaryLocation.Message).Returns("test message");

            var issueViz = CreateIssue(issue: issue.Object);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.Description.Should().Be("test message");
        }

        #endregion

        #region RuleKey

        [TestMethod]
        public void RuleKeyAndRuleDescriptionContextKey_CurrentIssueIsNull_Null()
        {
            testSubject.RuleKey.Should().BeNullOrEmpty();
            testSubject.RuleDescriptionContextKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKeyAndRuleDescriptionContextKey_CurrentIssueHasNoAnalysisIssue_Null()
        {
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.RuleKey.Should().BeNullOrEmpty();
            testSubject.RuleDescriptionContextKey.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void RuleKeyAndRuleDescriptionContextKey_CurrentIssueHasAnalysisIssue_IssueRuleKey()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test RuleKey");
            issue.SetupGet(x => x.RuleDescriptionContextKey).Returns("issue Context");

            var issueViz = CreateIssue(issue: issue.Object);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.RuleKey.Should().Be("test RuleKey");
            testSubject.RuleDescriptionContextKey.Should().Be("issue Context");
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
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            testSubject.Severity.Should().Be(DefaultNullIssueSeverity);
        }

        [TestMethod]
        public void Severity_CurrentIssueHasAnalysisIssue_IssueSeverity()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Severity).Returns(AnalysisIssueSeverity.Blocker);

            var issueViz = CreateIssue(issue: issue.Object);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

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
            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);
            var newIssue = CreateIssue(flows: new[] { newFlow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, newIssue, newFlow, newLocation);

            testSubject.CurrentIssue.Should().Be(newIssue);

            VerifyNotifyPropertyChanged("", nameof(testSubject.CurrentLocationListItem), nameof(testSubject.HasNonNavigableLocations));
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_PreviousIssueIsNotNull_NewIssueIsNull_CurrentIssueSetToNull()
        {
            var previousLocation = CreateLocation();
            var previousFlow = CreateFlow(previousLocation);
            var previousIssue = CreateIssue(flows: new[] { previousFlow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, previousIssue, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, null, null, null);

            testSubject.CurrentIssue.Should().BeNull();

            VerifyNotifyPropertyChanged("", nameof(testSubject.CurrentLocationListItem), nameof(testSubject.HasNonNavigableLocations));
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_PreviousIssueIsNotNull_NewIssueIsNotNull_CurrentIssueSetToNewValue()
        {
            var previousLocation = CreateLocation();
            var previousFlow = CreateFlow(previousLocation);
            var previousIssue = CreateIssue(flows: new[] { previousFlow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, previousIssue, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);
            var newIssue = CreateIssue(flows: new[] { newFlow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, newIssue, newFlow, newLocation);

            testSubject.CurrentIssue.Should().Be(newIssue);

            VerifyNotifyPropertyChanged("", nameof(testSubject.CurrentLocationListItem), nameof(testSubject.HasNonNavigableLocations));
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void SelectionService_OnIssueChanged_SelectionServiceNotCalled()
        {
            var issueViz = CreateIssue();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz);

            selectionServiceMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_UnregisterFromPreviousIssueAndRegisterToNewIssue()
        {
            var previousLocation = CreateLocation();
            var previousFlow = CreateFlow(previousLocation);
            var previousIssue = CreateIssue(flows: new[] { previousFlow });

            Mock.Get(previousIssue).SetupAdd(x => x.PropertyChanged += null);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, previousIssue, previousFlow, previousLocation);

            Mock.Get(previousIssue).VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);

            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);
            var newIssue = CreateIssue(flows: new[] { newFlow });

            Mock.Get(newIssue).SetupAdd(x => x.PropertyChanged += null);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, newIssue, newFlow, newLocation);

            Mock.Get(previousIssue).VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
            Mock.Get(newIssue).VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void SelectionService_OnIssueChanged_UnregisterFromPreviousLocationsAndRegisterToNewLocations()
        {
            var previousLocations = new[] { CreateLocation(), CreateLocation() };
            var previousFlow = CreateFlow(previousLocations);
            var previousIssue = CreateIssue(flows: new[] { previousFlow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, previousIssue, previousFlow, previousLocations.First());

            foreach (var previousLocation in previousLocations)
            {
                Mock.Get(previousLocation).VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);
            }

            var newLocations = new[] { CreateLocation(), CreateLocation() };
            var newFlow = CreateFlow(newLocations);
            var newIssue = CreateIssue(flows: new[] { newFlow });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, newIssue, newFlow, newLocations.First());

            foreach (var previousLocation in previousLocations)
            {
                Mock.Get(previousLocation).VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
            }
            foreach (var newLocation in newLocations)
            {
                Mock.Get(newLocation).VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);
            }
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
            var previousLocation = CreateLocation();
            var previousFlow = CreateFlow(previousLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, null);

            testSubject.CurrentFlow.Should().BeNull();

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentFlow), nameof(testSubject.LocationListItems), nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_PreviousFlowIsNull_NewFlowIsNotNull_CurrentFlowSetToNull()
        {
            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, newFlow, newLocation);

            testSubject.CurrentFlow.Should().Be(newFlow);

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentFlow), nameof(testSubject.LocationListItems), nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_PreviousFlowIsNotNull_NewFlowIsNotNull_CurrentFlowSetToNewValue()
        {
            var previousLocation = CreateLocation();
            var previousFlow = CreateFlow(previousLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, previousFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);

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
            selectionServiceMock.VerifySet(x => x.SelectedFlow = It.IsAny<IAnalysisIssueFlowVisualization>(), Times.Never());

            var selectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.CurrentFlow = selectedFlow;

            selectionServiceMock.VerifySet(x => x.SelectedFlow = selectedFlow, Times.Once());

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
        public void SelectionService_OnFlowChanged_PreviousListItemsAreDisposed()
        {
            var previousLocation = CreateLocation();
            var previousFlow = CreateFlow(previousLocation);
            var previousListItem = CreateMockFileNameListItem(previousLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, previousFlow);

            Mock.Get(previousListItem).Verify(x => x.Dispose(), Times.Never);

            var newLocation = CreateLocation();
            var newFlow = CreateFlow(newLocation);
            var newListItem = CreateMockFileNameListItem(newLocation);

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, newFlow);

            Mock.Get(previousListItem).Verify(x => x.Dispose(), Times.Once);
            Mock.Get(newListItem).Verify(x => x.Dispose(), Times.Never);
        }

        [TestMethod]
        public void SelectionService_OnFlowChanged_FlowWithOneLocation_LocationsListUpdated()
        {
            var location = CreateLocation("c:\\test\\c1.c");

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(new[] { location });

            var expectedLocationsList = new List<ILocationListItem>
            {
                CreateMockFileNameListItem(location),
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
                CreateLocation("c:\\test\\c1.c"),
                CreateLocation("c:\\c1.c"),
                CreateLocation("c:\\test\\c2.cpp"),
                CreateLocation("c:\\test\\c2.cpp"),
                CreateLocation("c:\\c3.h"),
                CreateLocation("c:\\c3.h"),
                CreateLocation("c:\\test\\c1.c")
            };

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(locations);

            var expectedLocationsList = new List<ILocationListItem>
            {
                CreateMockFileNameListItem(locations[0]),
                new LocationListItem(locations[0]),
                CreateMockFileNameListItem(locations[1]),
                new LocationListItem(locations[1]),
                CreateMockFileNameListItem(locations[2]),
                new LocationListItem(locations[2]),
                new LocationListItem(locations[3]),
                CreateMockFileNameListItem(locations[4]),
                new LocationListItem(locations[4]),
                new LocationListItem(locations[5]),
                CreateMockFileNameListItem(locations[6]),
                new LocationListItem(locations[6]),
            };

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object);

            VerifyLocationList(expectedLocationsList);
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
            var previousLocation = CreateLocation();
            var selectedFlow = CreateFlow(previousLocation);
            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow, previousLocation);
            propertyChangedEventHandler.Reset();

            RaiseSelectionChangedEvent(SelectionChangeLevel.Location, null, selectedFlow, null);

            testSubject.CurrentLocationListItem.Should().BeNull();

            VerifyNotifyPropertyChanged(nameof(testSubject.CurrentLocationListItem));
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_LocationIsNotInCurrentFlow_CurrentLocationSetToNull()
        {
            var previousLocation = CreateLocation();
            var selectedFlow = CreateFlow(previousLocation);
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
                CreateLocation("c:\\test\\c1.c"),
                CreateLocation("c:\\c1.c"),
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
            selectionServiceMock.VerifySet(x => x.SelectedLocation = It.IsAny<IAnalysisIssueLocationVisualization>(), Times.Never);

            var location = Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.CurrentLocationListItem = new LocationListItem(location);

            selectionServiceMock.VerifySet(x => x.SelectedLocation = location, Times.Once);
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

        [TestMethod]
        public void SetCurrentLocationListItem_NewLocationIsNull_NoNavigation()
        {
            testSubject.CurrentLocationListItem = null;

            locationNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetCurrentLocationListItem_NewLocationIsNotNull_NavigationToLocation()
        {
            var locationViz = CreateLocation("c:\\test\\c1.c");

            testSubject.CurrentLocationListItem = new LocationListItem(locationViz);

            locationNavigatorMock.Verify(x => x.TryNavigate(locationViz), Times.Once);
        }

        [TestMethod]
        public void SelectionService_OnLocationChanged_NoNavigation()
        {
            var locationViz = CreateLocation("c:\\test\\c1.c");

            var selectedFlow = new Mock<IAnalysisIssueFlowVisualization>();
            selectedFlow.Setup(x => x.Locations).Returns(new[] { locationViz });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Flow, null, selectedFlow.Object, locationViz);

            locationNavigatorMock.Verify(x => x.TryNavigate(It.IsAny<IAnalysisIssueLocationVisualization>()), Times.Never);
        }

        #endregion

        #region Dispose

        [TestMethod]
        public void Dispose_UnregisterFromEvents()
        {
            selectionServiceMock.SetupRemove(m => m.SelectionChanged -= (sender, args) => { });

            testSubject.Dispose();

            selectionServiceMock.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
            selectionServiceMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_CurrentIssueIsNotNull_UnregisterFromPropertyChangedEvent()
        {
            var issueViz = Mock.Get(CreateIssue());
            issueViz.SetupRemove(m => m.PropertyChanged -= (sender, args) => { });

            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issueViz.Object);

            issueViz.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Never);

            testSubject.Dispose();

            issueViz.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_CurrentIssueIsNotNull_UnregisterFromLocationPropertyChangedEvent()
        {
            var location = new Mock<IAnalysisIssueLocationVisualization>();
            location.SetupRemove(m => m.PropertyChanged -= (sender, args) => { });

            var flow = CreateFlow(location.Object);
            var issue = CreateIssue(flows: new[] { flow });
            RaiseSelectionChangedEvent(SelectionChangeLevel.Issue, issue);

            location.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Never);

            testSubject.Dispose();

            location.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
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

        private void VerifyNotifyPropertyChangedNotRaised()
        {
            propertyChangedEventHandler.Invocations.Count.Should().Be(0);
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

        private IAnalysisIssueLocationVisualization CreateLocation(string filePath = "test.cpp", SnapshotSpan? span = null)
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns(Mock.Of<IAnalysisIssueLocation>());
            locationViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            locationViz.Setup(x => x.Span).Returns(span);

            locationViz.SetupAdd(x => x.PropertyChanged += null);
            locationViz.SetupRemove(x => x.PropertyChanged -= null);

            return locationViz.Object;
        }

        private IAnalysisIssueFlowVisualization CreateFlow(params IAnalysisIssueLocationVisualization[] locations)
        {
            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locations);

            return flowViz.Object;
        }

        private IAnalysisIssueVisualization CreateIssue(
            SnapshotSpan? span = null,
            string filePath = null,
            IAnalysisIssueBase issue = null,
            params IAnalysisIssueFlowVisualization[] flows)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue);
            issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            issueViz.Setup(x => x.Span).Returns(span);
            issueViz.Setup(x => x.Flows).Returns(flows);

            return issueViz.Object;
        }

        private IFileNameLocationListItem CreateMockFileNameListItem(IAnalysisIssueLocationVisualization location)
        {
            var listItem = Mock.Of<IFileNameLocationListItem>();

            fileNameLocationListItemCreatorMock
                .Setup(x => x.Create(location))
                .Returns(listItem);

            return listItem;
        }

        private void VerifyLocationList(IEnumerable<ILocationListItem> expectedLocationsList)
        {
            testSubject.LocationListItems.Should().BeEquivalentTo(expectedLocationsList, assertionOptions =>
                assertionOptions
                    .WithStrictOrdering()
                    .RespectingRuntimeTypes()); // check underlying types rather than ILocationListItem
        }

        private void RaisePropertyChangedEvent(Mock<IAnalysisIssueVisualization> issueViz, string propertyName)
        {
            issueViz.Raise(x => x.PropertyChanged += null, null, new PropertyChangedEventArgs(propertyName));
        }

        private void RaisePropertyChangedEvent(Mock<IAnalysisIssueLocationVisualization> locationViz, string propertyName)
        {
            locationViz.Raise(x => x.PropertyChanged += null, null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
