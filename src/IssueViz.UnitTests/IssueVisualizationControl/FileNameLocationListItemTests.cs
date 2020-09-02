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
using System.ComponentModel;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.IssueVisualizationControl
{
    [TestClass]
    public class FileNameLocationListItemTests
    {
        private Mock<IVsImageService2> imageServiceMock;
        private TestLogger logger;
        private Mock<PropertyChangedEventHandler> propertyChangedEventHandler;

        [TestInitialize]
        public void TestInitialize()
        {
            imageServiceMock = new Mock<IVsImageService2>();
            logger = new TestLogger();
            propertyChangedEventHandler = new Mock<PropertyChangedEventHandler>();
        }

        [TestMethod]
        public void Ctor_InitializeWithLocationData()
        {
            var location = CreateMockLocation("c:\\test.cpp", KnownMonikers.CPPFile);

            var testSubject = CreateTestSubject(location.Object);

            testSubject.FileName.Should().Be("test.cpp");
            testSubject.FullPath.Should().Be("c:\\test.cpp");
            testSubject.Icon.Should().BeEquivalentTo(KnownMonikers.CPPFile, c=> c.ComparingByMembers<ImageMoniker>());
        }

        [TestMethod]
        public void Ctor_FailsToRetrieveFileIcon_BlankIcon()
        {
            var location = CreateMockLocation("c:\\test\\c1.c", KnownMonikers.CFile, new NotImplementedException("this is a test"));

            var testSubject = CreateTestSubject(location.Object);

            testSubject.Icon.Should().BeEquivalentTo(KnownMonikers.Blank, c => c.ComparingByMembers<ImageMoniker>());

            logger.AssertPartialOutputStringExists("this is a test");
            logger.OutputStrings.Count.Should().Be(1);
        }

        [TestMethod]
        public void Dispose_UnregisterFromLocationEvents()
        {
            var location = CreateMockLocation("c:\\test.cpp", KnownMonikers.CPPFile);
            location.SetupRemove(m => m.PropertyChanged -= (sender, args) => { });

            var testSubject = CreateTestSubject(location.Object);

            location.Invocations.Clear();
            testSubject.Dispose();

            location.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
            location.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void LocationPropertyChanged_NonFilePathProperty_NoChanges()
        {
            var location = CreateMockLocation("c:\\old file.cpp", KnownMonikers.CPPFile);
            var testSubject = CreateTestSubject(location.Object);

            location.Object.CurrentFilePath = "c:\\should-not-be-queried.cpp";
            location.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("dummy property"));

            testSubject.FileName.Should().Be("old file.cpp");
            testSubject.FullPath.Should().Be("c:\\old file.cpp");
            testSubject.Icon.Should().BeEquivalentTo(KnownMonikers.CPPFile, c => c.ComparingByMembers<ImageMoniker>());

            propertyChangedEventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void LocationPropertyChanged_FilePathProperty_AllPropertiesAreUpdated()
        {
            const string oldFilePath = "c:\\old.cpp";
            const string newFilePath = "c:\\new.c";
            object oldIcon = KnownMonikers.CPPFile;
            object newIcon = KnownMonikers.CFile;

            var location = CreateMockLocation(oldFilePath, oldIcon);
            var testSubject = CreateTestSubject(location.Object);

            imageServiceMock.Setup(x => x.GetImageMonikerForFile(newFilePath)).Returns((ImageMoniker)newIcon);

            location.Object.CurrentFilePath = newFilePath;
            location.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueLocationVisualization.CurrentFilePath)));

            propertyChangedEventHandler.Verify(x =>
                    x(It.IsAny<object>(), It.Is((PropertyChangedEventArgs e) => e.PropertyName == string.Empty)),
                Times.Once);

            testSubject.FileName.Should().Be(Path.GetFileName(newFilePath));
            testSubject.FullPath.Should().Be(newFilePath);
            testSubject.Icon.Should().BeEquivalentTo(newIcon, c => c.ComparingByMembers<ImageMoniker>());
        }

        private Mock<IAnalysisIssueLocationVisualization> CreateMockLocation(string filePath, object imageMoniker, Exception failsToRetrieveMoniker = null)
        {
            if (failsToRetrieveMoniker != null)
            {
                imageServiceMock
                    .Setup(x => x.GetImageMonikerForFile(filePath))
                    .Throws(failsToRetrieveMoniker);
            }
            else
            {
                imageServiceMock.Setup(x => x.GetImageMonikerForFile(filePath)).Returns((ImageMoniker) imageMoniker);
            }

            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.SetupProperty(x => x.CurrentFilePath);

            locationViz.Object.CurrentFilePath = filePath;

            return locationViz;
        }

        private FileNameLocationListItem CreateTestSubject(IAnalysisIssueLocationVisualization location)
        {
            var testSubject = new FileNameLocationListItem(location, imageServiceMock.Object, logger);
            testSubject.PropertyChanged += propertyChangedEventHandler.Object;

            return testSubject;
        }
    }
}
