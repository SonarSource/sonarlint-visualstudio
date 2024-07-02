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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Analysis
{
    [TestClass]
    public class IssueConsumerStorageTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueConsumerStorage, IIssueConsumerStorage>();
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<IssueConsumerStorage>();
        }

        [TestMethod]
        public void Remove_RemovesExistingItem()
        {
            var testSubject = CreateTestSubject();

            testSubject.internalStorage.Add("Key", (Guid.NewGuid(), Substitute.For<IIssueConsumer>()));

            testSubject.Remove("Key");

            testSubject.internalStorage.Should().BeEmpty();
        }

        [TestMethod]
        public void Remove_HasMultipleItems_RemovesCorrectItem()
        {
            var testSubject = CreateTestSubject();

            testSubject.internalStorage.Add("Key1", (Guid.NewGuid(), Substitute.For<IIssueConsumer>()));
            testSubject.internalStorage.Add("Key2", (Guid.NewGuid(), Substitute.For<IIssueConsumer>()));

            testSubject.Remove("Key2");

            testSubject.internalStorage.Should().HaveCount(1);
            testSubject.internalStorage.ContainsKey("Key1").Should().BeTrue();
        }

        [TestMethod]
        public void Remove_HasNoItems_DoNotThrow()
        {
            var testSubject = CreateTestSubject();

            testSubject.Remove("Key");

            testSubject.internalStorage.Should().BeEmpty();
        }

        [TestMethod]
        public void Set_ItemIsNotInStorage_AddsItem()
        {
            var testSubject = CreateTestSubject();

            testSubject.internalStorage.Add("Key1", (Guid.NewGuid(), Substitute.For<IIssueConsumer>()));

            var guid = Guid.NewGuid();
            var consumer = Substitute.For<IIssueConsumer>();

            testSubject.Set("Key2", guid, consumer);

            testSubject.internalStorage.Should().HaveCount(2);
            testSubject.internalStorage.ContainsKey("Key2").Should().BeTrue();
            testSubject.internalStorage["Key2"].analysisID.Should().Be(guid);
            testSubject.internalStorage["Key2"].consumer.Should().Be(consumer);
        }

        [TestMethod]
        public void Set_ItemIsNotTnStorage_RefreshItem()
        {
            var testSubject = CreateTestSubject();

            testSubject.internalStorage.Add("Key1", (Guid.NewGuid(), Substitute.For<IIssueConsumer>()));
            testSubject.internalStorage.Add("Key2", (Guid.NewGuid(), Substitute.For<IIssueConsumer>()));

            var guid = Guid.NewGuid();
            var consumer = Substitute.For<IIssueConsumer>();

            testSubject.Set("Key2", guid, consumer);

            testSubject.internalStorage.Should().HaveCount(2);
            testSubject.internalStorage.ContainsKey("Key2").Should().BeTrue();
            testSubject.internalStorage["Key2"].analysisID.Should().Be(guid);
            testSubject.internalStorage["Key2"].consumer.Should().Be(consumer);
        }

        [TestMethod]
        public void TryGet_ItemOnStorage_ReturnsTrue()
        {
            var testSubject = CreateTestSubject();

            var guid = Guid.NewGuid();
            var consumer = Substitute.For<IIssueConsumer>();

            testSubject.internalStorage.Add("Key", (guid, consumer));

            var result = testSubject.TryGet("Key", out var outGuid, out var outConsumer);

            result.Should().BeTrue();
            outGuid.Should().Be(guid);
            outConsumer.Should().Be(consumer);
        }

        [TestMethod]
        public void TryGet_ItemNotOnStorage_ReturnsFalse()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.TryGet("Key", out var outGuid, out var outConsumer);

            result.Should().BeFalse();
            outGuid.Should().BeEmpty();
            outConsumer.Should().BeNull();
        }

        private IssueConsumerStorage CreateTestSubject() => new();
    }
}
