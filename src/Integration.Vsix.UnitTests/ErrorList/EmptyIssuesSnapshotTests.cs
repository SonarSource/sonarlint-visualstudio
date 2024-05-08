/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Linq;
using Microsoft.VisualStudio.Shell.TableManager;
using static SonarLint.VisualStudio.Integration.Vsix.ErrorList.IssuesSnapshotFactory;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class EmptyIssuesSnapshotTests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow(" \t")]
        [DataRow(null)]
        public void Create_MissingFilePath_Throws(string filePath)
        {
            Action act = () => CreateEmptySnapshot(filePath);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("filePath");
        }

        [TestMethod]
        public void Create_AnalysisRunId_IsNotEmptyGuid()
            => CreateEmptySnapshot("any").AnalysisRunId.Should().NotBe(Guid.Empty);

        [TestMethod]
        public void Create_AnalyzedFilePath()
            => CreateEmptySnapshot("c:\\a\\b.txt").AnalyzedFilePath.Should().Be("c:\\a\\b.txt");

        [TestMethod]
        public void EmptyIssuesSnapshot_FilesInSnapshot_IsExpected() 
            => CreateEmptySnapshot("my file").FilesInSnapshot.Should().BeEquivalentTo(new[] { "my file" });

        [TestMethod]
        public void EmptyIssuesSnapshot_CreateUpdateShapshot_UpdatesFileName()
        {
            var testSubject = CreateEmptySnapshot("file1");
            testSubject.AnalyzedFilePath.Should().Be("file1");

            var updatedSnapshot = testSubject.CreateUpdatedSnapshot("file2");
            updatedSnapshot.AnalyzedFilePath.Should().Be("file2");
            updatedSnapshot.Should().BeSameAs(testSubject);
        }

        [TestMethod]
        public void EmptyIssuesSnapshot_GetUpdatedShapshot_ReturnsExpected()
        {
            var testSubject = CreateEmptySnapshot("any");

            var updatedSnapshot = testSubject.GetUpdatedSnapshot();
            updatedSnapshot.Should().BeSameAs(testSubject);
        }

        [TestMethod]
        public void EmptyIssuesSnapshot_IsEmpty()
        {
            var testSubject = CreateEmptySnapshot("my file");

            testSubject.Count.Should().Be(0);
            testSubject.Issues.Count().Should().Be(0);
            testSubject.GetLocationsVizsForFile("my file").Count().Should().Be(0);
        }

        #region ITableEntriesSnapshot methods

        [TestMethod]
        public void EmptyIssuesSnapshot_ITableEntriesSnapshot_Count_ReturnsZero()
            => CreateEmptySnapshot("any").Count.Should().Be(0);

        [TestMethod]
        public void EmptyIssuesSnapshot_ITableEntriesSnapshot_VersionNumber_ReturnsExpected()
            => CreateEmptySnapshot("any").VersionNumber.Should().Be(-1);

        [TestMethod]
        public void EmptyIssuesSnapshot_ITableEntriesSnapshot_IndexOf_ReturnsExpected()
            => CreateEmptySnapshot("any").IndexOf(100, Mock.Of<ITableEntriesSnapshot>()).Should().Be(-1);
        
        [TestMethod]
        [DataRow(0, StandardTableKeyNames.DocumentName)]
        [DataRow(1, StandardTableKeyNames.Column)]
        [DataRow(2, "any")]
        public void EmptyIssuesSnapshot_ITableEntriesSnapshot_TryGetValue_ReturnsFalse(int index, string keyName)
        {
            var testSubject = CreateEmptySnapshot("my file");

            var result = testSubject.TryGetValue(index, keyName, out var content);
            result.Should().BeFalse();
            content.Should().BeNull();
        }

        #endregion ITableEntriesSnapshot methods
    }
}
