﻿/*
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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.IssueVisualization.TableControls;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class SonarErrorListDataSourceTests
    {
        private Mock<ITableManagerProvider> mockTableManagerProvider;
        private Mock<ITableManager> mockTableManager;
        private IIssuesSnapshotFactory ValidFactory;

        [TestInitialize]
        public void TestInitialize()
        {
            ValidFactory = Mock.Of<IIssuesSnapshotFactory>();
            Mock.Get(ValidFactory).SetupGet(x => x.CurrentSnapshot).Returns(Mock.Of<IIssuesSnapshot>());

            mockTableManagerProvider = new Mock<ITableManagerProvider>();
            mockTableManager = new Mock<ITableManager>();
            mockTableManagerProvider.Setup(x => x.GetTableManager(StandardTables.ErrorsTable)).Returns(mockTableManager.Object);
        }

        [TestMethod]
        public void MefCtor_CheckExports()
        {
            CompositionBatch batch = new CompositionBatch();

            // Set up the exports required by the test subject
            var fileRenamesEventSourceExport = MefTestHelpers.CreateExport<IFileRenamesEventSource>(Mock.Of<IFileRenamesEventSource>());
            batch.AddExport(fileRenamesEventSourceExport);

            var tableManagerExport = MefTestHelpers.CreateExport<ITableManagerProvider>(mockTableManagerProvider.Object);
            batch.AddExport(tableManagerExport);

            var selectionServiceExport = MefTestHelpers.CreateExport<IIssueSelectionService>(Mock.Of<IIssueSelectionService>());
            batch.AddExport(selectionServiceExport);

            // Set up importers for each of the interfaces exported by the test subject
            var errorDataSourceImporter = new SingleObjectImporter<ISonarErrorListDataSource>();
            var issueLocationStoreImporter = new SingleObjectImporter<IIssueLocationStore>();
            batch.AddPart(errorDataSourceImporter);
            batch.AddPart(issueLocationStoreImporter);

            // Specify the source types that can be used to satify any import requests
            TypeCatalog catalog = new TypeCatalog(typeof(SonarErrorListDataSource));

            using (CompositionContainer container = new CompositionContainer(catalog))
            {
                container.Compose(batch);

                // Both imports should be satisfied...
                errorDataSourceImporter.Import.Should().NotBeNull();
                issueLocationStoreImporter.Import.Should().NotBeNull();

                // ... and the the export should be a singleton, so the both importers should
                // get the same instance
                errorDataSourceImporter.Import.Should().BeSameAs(issueLocationStoreImporter.Import);
            }
        }

        [TestMethod]
        public void Ctor_WithInvalidArgs_Throws()
        {
            Action act = () => new SonarErrorListDataSource(null, Mock.Of<IFileRenamesEventSource>(), Mock.Of<IIssueSelectionService>());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("tableManagerProvider");

            act = () => new SonarErrorListDataSource(mockTableManagerProvider.Object, null, Mock.Of<IIssueSelectionService>());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileRenamesEventSource");

            act = () => new SonarErrorListDataSource(mockTableManagerProvider.Object,  Mock.Of<IFileRenamesEventSource>(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issueSelectionService");
        }

        [TestMethod]
        public void Ctor_FetchesErrorTableManagerAndRegistersDataSourceAndTableColumnDefinitions()
        {
            var tableColumnDefinitions = new[] { StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                                   StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column,
                                                   StandardTableColumnDefinitions.ProjectName,
                                                   SuppressionsColumnHelper.SuppressionStateColumnName};
            var testSubject = CreateTestSubject();

            mockTableManager.Verify(x => x.AddSource(testSubject, tableColumnDefinitions), Times.Once);
        }

        [TestMethod]
        public void DataSourceIdentifierIsCorrect()
        {
            var testSubject = CreateTestSubject();

            testSubject.Identifier.Should().Be(SonarLintTableControlConstants.ErrorListDataSourceIdentifier);
        }

        [TestMethod]
        public void Subscribe_ReturnsNewDisposableToken()
        {
            var mockSink = new Mock<ITableDataSink>();
            var testSubject = CreateTestSubject();

            var token = testSubject.Subscribe(mockSink.Object);

            token.Should().NotBeNull();
            token.Should().BeOfType<ExecuteOnDispose>();
        }

        [TestMethod]
        public void Refresh_NullArg_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.RefreshErrorList(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("factory");
        }

        [TestMethod]
        public void Refresh_NoSubscribedSinks_NoError()
        {
            var testSubject = CreateTestSubjectWithFactory(ValidFactory);

            Action act = () => testSubject.RefreshErrorList(ValidFactory);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Refresh_FactoryIsNotRegistered_SinksAreNotNotified()
        {
            var mockSink = new Mock<ITableDataSink>();
            var testSubject = CreateTestSubjectWithFactory(ValidFactory);
            testSubject.Subscribe(mockSink.Object);

            var unknownFactory = Mock.Of<IIssuesSnapshotFactory>();

            testSubject.RefreshErrorList(unknownFactory);

            CheckSinkWasNotNotified(mockSink);
        }

        [TestMethod]
        public void Refresh_OnlySubscribedSinksAreNotified()
        {
            var mockSink1 = new Mock<ITableDataSink>();
            var mockSink2 = new Mock<ITableDataSink>();
            var testSubject = CreateTestSubjectWithFactory(ValidFactory);

            var sink1Token = testSubject.Subscribe(mockSink1.Object);
            testSubject.Subscribe(mockSink2.Object);

            // 1. Refresh -> both sinks notified
            testSubject.RefreshErrorList(ValidFactory);

            CheckSinkWasNotified(mockSink1, ValidFactory);
            CheckSinkWasNotified(mockSink2, ValidFactory);

            // 2. Unregister one sink then refresh
            mockSink1.Reset();
            mockSink2.Reset();
            sink1Token.Dispose();

            testSubject.RefreshErrorList(ValidFactory);

            CheckSinkWasNotNotified(mockSink1);
            CheckSinkWasNotified(mockSink2, ValidFactory);
        }

        [TestMethod]
        public void Subscribe_NewManagerIsNotifiedOfExistingFactories()
        {
            var testSubject = CreateTestSubject();

            var factory1 = Mock.Of<IIssuesSnapshotFactory>();
            var factory2 = Mock.Of<IIssuesSnapshotFactory>();
            testSubject.AddFactory(factory1);
            testSubject.AddFactory(factory2);

            var mockSinkManager = new Mock<ITableDataSink>();

            testSubject.Subscribe(mockSinkManager.Object);

            CheckFactoryWasAdded(mockSinkManager, factory1);
            CheckFactoryWasAdded(mockSinkManager, factory2);
        }

        [TestMethod]
        public void AddFactory_ExistingSinkManagersAreNotifiedOfNewFactory()
        {
            var testSubject = CreateTestSubject();

            var mockSink1 = new Mock<ITableDataSink>();
            var mockSink2 = new Mock<ITableDataSink>();
            testSubject.Subscribe(mockSink1.Object);
            testSubject.Subscribe(mockSink2.Object);

            var factory = Mock.Of<IIssuesSnapshotFactory>();

            testSubject.AddFactory(factory);

            CheckFactoryWasAdded(mockSink1, factory);
            CheckFactoryWasAdded(mockSink2, factory);
        }

        [TestMethod]
        public void RemoveSink_IsNoLongerNotified()
        {
            var testSubject = CreateTestSubjectWithFactory(ValidFactory);

            var mockSink = new Mock<ITableDataSink>();
            var disposeToken = testSubject.Subscribe(mockSink.Object);

            // 1. Manager is registered -> should be notified
            testSubject.RefreshErrorList(ValidFactory);
            testSubject.RefreshErrorList(ValidFactory);

            mockSink.Verify(x => x.FactorySnapshotChanged(ValidFactory), Times.Exactly(2));

            // 2. Unsubscribe -> no longer notified
            mockSink.Reset();
            disposeToken.Dispose();
            testSubject.RefreshErrorList(ValidFactory);

            mockSink.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Never);
        }

        [TestMethod]
        public void RemoveFactory_ExistingSinkManagersAreNotified()
        {
            var testSubject = CreateTestSubject();

            var mockSink1 = new Mock<ITableDataSink>();
            var mockSink2 = new Mock<ITableDataSink>();
            testSubject.Subscribe(mockSink1.Object);
            testSubject.Subscribe(mockSink2.Object);

            var factory = new Mock<IIssuesSnapshotFactory>();
            factory.SetupGet(x => x.CurrentSnapshot).Returns(Mock.Of<IIssuesSnapshot>());

            testSubject.RemoveFactory(factory.Object);

            CheckFactoryWasRemoved(mockSink1, factory.Object);
            CheckFactoryWasRemoved(mockSink2, factory.Object);
        }

        #region Exception handling tests

        [TestMethod]
        public void CallsToSink_AddFactory_NonCriticalException_Suppressed()
        {
            // Arrange
            var mockSink = new Mock<ITableDataSink>();
            mockSink.Setup(x => x.AddFactory(ValidFactory, false))
                .Throws(new InvalidCastException("add factory custom error"));

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(mockSink.Object);

            // Act
            testSubject.AddFactory(ValidFactory);

            // Assert
            CheckFactoryWasAdded(mockSink, ValidFactory);
        }

        [TestMethod]
        public void CallsToSink_RemoveFactory_NonCriticalException_Suppressed()
        {
            // Arrange
            var mockSink = new Mock<ITableDataSink>();
            mockSink.Setup(x => x.RemoveFactory(ValidFactory))
                .Throws(new InvalidCastException("remove factory custom error"));

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(mockSink.Object);

            // Act
            testSubject.RemoveFactory(ValidFactory);

            // Assert
            CheckFactoryWasRemoved(mockSink, ValidFactory);
        }

        [TestMethod]
        public void CallsToSink_RefreshErrorList_NonCriticalException_Suppressed()
        {
            // Arrange
            var mockSink = new Mock<ITableDataSink>();
            mockSink.Setup(x => x.FactorySnapshotChanged(null))
                .Throws(new InvalidCastException("update custom error"));

            var testSubject = CreateTestSubjectWithFactory(ValidFactory);
            testSubject.Subscribe(mockSink.Object);

            // Act
            testSubject.RefreshErrorList(ValidFactory);

            // Assert
            CheckSinkWasNotified(mockSink, ValidFactory);
        }

        [TestMethod]
        public void CallsToSink_AddFactory_CriticalException_NotSuppressed()
        {
            // Arrange
            var mockSink = new Mock<ITableDataSink>();
            mockSink.Setup(x => x.AddFactory(ValidFactory, false))
                .Throws(new StackOverflowException("add factory custom error"));

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(mockSink.Object);

            // Act & assert
            Action act = () => testSubject.AddFactory(ValidFactory);
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("add factory custom error");
        }

        [TestMethod]
        public void CallsToSink_RemoveFactory_CriticalException_NotSuppressed()
        {
            // Arrange
            var mockSink = new Mock<ITableDataSink>();
            mockSink.Setup(x => x.RemoveFactory(ValidFactory))
                .Throws(new StackOverflowException("remove factory custom error"));

            var testSubject = CreateTestSubject();
            testSubject.Subscribe(mockSink.Object);

            // Act & assert
            Action act = () => testSubject.RemoveFactory(ValidFactory);
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("remove factory custom error");
        }

        [TestMethod]
        public void CallsToSink_RefreshErrorList_CriticalException_NotSuppressed()
        {
            // Arrange
            var mockSink = new Mock<ITableDataSink>();
            mockSink.Setup(x => x.FactorySnapshotChanged(ValidFactory))
                .Throws(new StackOverflowException("update custom error"));

            var testSubject = CreateTestSubjectWithFactory(ValidFactory);
            testSubject.Subscribe(mockSink.Object);

            // Act & assert
            Action act = () => testSubject.RefreshErrorList(ValidFactory);
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("update custom error");
        }

        #endregion

        private SonarErrorListDataSource CreateTestSubjectWithFactory(IIssuesSnapshotFactory factory)
        {
            var testSubject = CreateTestSubject();
            testSubject.AddFactory(factory);
            return testSubject;
        }

        private SonarErrorListDataSource CreateTestSubject()
        {
            return new SonarErrorListDataSource(mockTableManagerProvider.Object,
                Mock.Of<IFileRenamesEventSource>(),
                Mock.Of<IIssueSelectionService>());
        }

        private static void CheckSinkWasNotified(Mock<ITableDataSink> mockSink, IIssuesSnapshotFactory expectedFactory) =>
            mockSink.Verify(x => x.FactorySnapshotChanged(expectedFactory), Times.Once);

        private static void CheckSinkWasNotNotified(Mock<ITableDataSink> mockSink) =>
            mockSink.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Never);

        private static void CheckFactoryWasAdded(Mock<ITableDataSink> mockSink, ITableEntriesSnapshotFactory expectedFactory) =>
            mockSink.Verify(x => x.AddFactory(expectedFactory, false), Times.Once);

        private static void CheckFactoryWasRemoved(Mock<ITableDataSink> mockSink, ITableEntriesSnapshotFactory expectedFactory) =>
            mockSink.Verify(x => x.RemoveFactory(expectedFactory), Times.Once);
    }
}
