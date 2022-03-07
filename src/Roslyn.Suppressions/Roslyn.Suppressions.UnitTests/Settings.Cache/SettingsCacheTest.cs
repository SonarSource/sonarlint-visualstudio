/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarQube.Client.Models;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;


namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.Settings.Cache
{
    [TestClass]
    public class SettingsCacheTest
    {
        [TestMethod]
        public void GetSettings_SettingNotInCache_SettingsReadFromFile()
        {
            var issues = CreateIssues();

            var cacheObject = new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>();

            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();
            fileStorage.Setup(fs => fs.Get("settingsKey")).Returns(issues);

            var testSubject = CreateTestSubject(fileStorage, cacheObject);
            var settings = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get("settingsKey"), Times.Once);
            cacheObject.ContainsKey("settingsKey").Should().BeTrue();
            cacheObject["settingsKey"].Should().BeSameAs(issues);
            settings.Should().BeSameAs(issues);
        }

        [TestMethod]
        public void GetSettings_SettingInCache_SettingsReadFromCache()
        {
            var issues = CreateIssues();

            var cacheObject = CreatePopulatedCacheObject("settingsKey", issues);

            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();

            var testSubject = CreateTestSubject(fileStorage, cacheObject);
            var settings = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get(It.IsAny<string>()), Times.Never);
            settings.Should().BeSameAs(issues);
        }

        [TestMethod]
        public void GetSettings_SettingNotInCacheAndFile_SettingsEmpty()
        {
            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();

            var testSubject = CreateTestSubject(fileStorage);
            var settings = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get("settingsKey"), Times.Once);
            settings.Count().Should().Be(0);
        }

        [TestMethod]
        public void GetSettings_DifferentSettingInCache_SettingsEmpty()
        {
            var issues = CreateIssues();

            var cacheObject = CreatePopulatedCacheObject("differentKey", issues);

            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();

            var testSubject = CreateTestSubject(fileStorage, cacheObject);
            var settings = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get("settingsKey"), Times.Once);
            settings.Count().Should().Be(0);
        }

        [TestMethod]
        public void Invalidate_SettingInCache_SettingsRemovedFromCache()
        {
            var issues = CreateIssues();

            var cacheObject = CreatePopulatedCacheObject("settingsKey", issues);


            var testSubject = CreateTestSubject(settingsCollection: cacheObject);
            testSubject.Invalidate("settingsKey");

            cacheObject.ContainsKey("settingsKey").Should().BeFalse();
        }
        [TestMethod]
        public void Invalidate_SettingNotInCache_NoErrorThrown()
        {
            var testSubject = CreateTestSubject();

            testSubject.Invalidate("settingsKey");
        }

        private static ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> CreatePopulatedCacheObject(string settingsKey, IEnumerable<SonarQubeIssue> issues)
        {            
            var cacheObject = new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>();
            cacheObject.AddOrUpdate(settingsKey, issues, (x, y) => issues);

            return cacheObject;
        }

        private IEnumerable<SonarQubeIssue> CreateIssues()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");
            return new List<SonarQubeIssue> { issue1, issue2 };
        }

        private SettingsCache CreateTestSubject(Mock<ISuppressedIssuesFileStorage> fileStorage = null, ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> settingsCollection = null)
        {
            fileStorage = fileStorage ?? new Mock<ISuppressedIssuesFileStorage>();
            settingsCollection = settingsCollection ?? new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>();

            return new SettingsCache(fileStorage.Object, settingsCollection);
        }
    }
}
