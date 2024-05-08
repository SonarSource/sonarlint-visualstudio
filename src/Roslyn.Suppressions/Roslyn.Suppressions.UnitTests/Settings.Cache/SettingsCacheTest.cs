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

using System.Collections.Concurrent;
using System.Linq;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarLint.VisualStudio.TestInfrastructure.Helpers;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.Settings.Cache
{
    [TestClass]
    public class SettingsCacheTest
    {
        [TestMethod]
        public void GetSettings_SettingNotInCache_SettingsReadFromFile()
        {
            var settings = new RoslynSettings { SonarProjectKey = "my project" };
            var cacheObject = CreateEmptyCacheObject();

            var fileStorage = new Mock<IRoslynSettingsFileStorage>();
            fileStorage.Setup(fs => fs.Get("settingsKey")).Returns(settings);

            var testSubject = CreateTestSubject(fileStorage, cacheObject);
            var actual = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get("settingsKey"), Times.Once);
            cacheObject.ContainsKey("settingsKey").Should().BeTrue();
            cacheObject["settingsKey"].Should().BeSameAs(settings);
            actual.Should().BeSameAs(settings);
        }

        [DataRow("settingskey", "settingskey")]//Testing lower case
        [DataRow("settingsKEY", "settingskey")]//Testing upper case
        [DataRow("III", "iii")]//Testing upper case with invariant culture
        [TestMethod]
        public void GetSettings_SettingInCache_SettingsReadFromCache(string settingsKey, string normalisedKey)
        {
            //This is to make sure normalising the keys done correctly with invariant culture
            //https://en.wikipedia.org/wiki/Dotted_and_dotless_I 
            using var scope = new TurkishCultureScope();

            var settings = new RoslynSettings { SonarProjectKey = "my project" };

            var cacheObject = CreatePopulatedCacheObject(normalisedKey, settings);

            var fileStorage = new Mock<IRoslynSettingsFileStorage>();

            var testSubject = CreateTestSubject(fileStorage, cacheObject);
            var actual = testSubject.GetSettings(settingsKey);

            fileStorage.Verify(fs => fs.Get(It.IsAny<string>()), Times.Never);
            actual.Should().BeSameAs(settings);
            
        }

        [TestMethod]
        public void GetSettings_SettingNotInCacheOrFile_SettingsEmpty()
        {
            var fileStorage = new Mock<IRoslynSettingsFileStorage>();

            var testSubject = CreateTestSubject(fileStorage);
            var actual = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get("settingsKey"), Times.Once);
            CheckSettingsAreEmpty(actual);
        }

        [TestMethod]
        public void GetSettings_DifferentSettingInCache_SettingsEmpty()
        {
            var settings = new RoslynSettings { SonarProjectKey = "my project" };

            var cacheObject = CreatePopulatedCacheObject("differentkey", settings);

            var fileStorage = new Mock<IRoslynSettingsFileStorage>();

            var testSubject = CreateTestSubject(fileStorage, cacheObject);
            var actual = testSubject.GetSettings("settingsKey");

            fileStorage.Verify(fs => fs.Get("settingsKey"), Times.Once);
            CheckSettingsAreEmpty(actual);
        }

        [DataRow("settingskey", "settingskey")]//Testing lower case
        [DataRow("settingsKEY", "settingskey")]//Testing upper case
        [DataRow("III","iii")]//Testing upper case with invariant culture
        [TestMethod]
        public void Invalidate_SettingInCache_SettingsRemovedFromCache(string settingsKey, string normalisedKey)
        {
            //This is to make sure normalising the keys done correctly with invariant culture
            //https://en.wikipedia.org/wiki/Dotted_and_dotless_I 
            using var scope = new TurkishCultureScope();
            
            var cacheObject = CreatePopulatedCacheObject(normalisedKey, new RoslynSettings());
            cacheObject.ContainsKey(normalisedKey).Should().BeTrue("Test setup error: cache was not pre-populated correctly");

            var testSubject = CreateTestSubject(settingsCollection: cacheObject);
            testSubject.Invalidate(settingsKey);

            cacheObject.ContainsKey(normalisedKey).Should().BeFalse();
            
        }
        [TestMethod]
        public void Invalidate_SettingNotInCache_NoErrorThrown()
        {
            var testSubject = CreateTestSubject();

            testSubject.Invalidate("settingsKey");
        }

        private ConcurrentDictionary<string, RoslynSettings> CreatePopulatedCacheObject(string settingsKey, RoslynSettings settings)
        {
            var cacheObject = CreateEmptyCacheObject();
            cacheObject.AddOrUpdate(settingsKey, settings, (x, y) => settings);

            return cacheObject;
        }


        private SettingsCache CreateTestSubject(Mock<IRoslynSettingsFileStorage> fileStorage = null, ConcurrentDictionary<string, RoslynSettings> settingsCollection = null)
        {
            fileStorage = fileStorage ?? new Mock<IRoslynSettingsFileStorage>();
            settingsCollection = settingsCollection ?? new ConcurrentDictionary<string, RoslynSettings>(StringComparer.OrdinalIgnoreCase);

            return new SettingsCache(fileStorage.Object, settingsCollection);
        }

        private ConcurrentDictionary<string, RoslynSettings> CreateEmptyCacheObject()
        {
            return new ConcurrentDictionary<string, RoslynSettings>(StringComparer.OrdinalIgnoreCase);
        }

        private static void CheckSettingsAreEmpty(RoslynSettings settings)
        {
            settings.Should().BeSameAs(RoslynSettings.Empty);

            settings.SonarProjectKey.Should().BeNull();
            settings.Suppressions.Should().NotBeNull();
            settings.Suppressions.Count().Should().Be(0);
        }
    }
}
