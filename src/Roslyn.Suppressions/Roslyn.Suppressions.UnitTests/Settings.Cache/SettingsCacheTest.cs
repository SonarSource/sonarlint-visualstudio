using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.Settings.Cache
{
    [TestClass]
    public class SettingsCacheTest
    {
        [TestMethod]
        public void GetSettings_SettingNotInCache_SettingsReadFromFile()
        {
            var issues = CreateIssues();

            var cacheObject = CreateCacheObject();

            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();
            fileStorage.Setup(fs => fs.Get("projectKey")).Returns(issues);
            
            var settingsSingleton = CreateSettingsSingleton(cacheObject);

            var testSubject = CreateTestSubject(fileStorage, settingsSingleton);
            var settings = testSubject.GetSettings("projectKey");

            fileStorage.Verify(fs => fs.Get("projectKey"), Times.Once);
            cacheObject.ContainsKey("projectKey").Should().BeTrue();
            cacheObject["projectKey"].Should().BeSameAs(issues);
            settings.Should().BeSameAs(issues);
        }

        [TestMethod]
        public void GetSettings_SettingInCache_SettingsReadFromCache()
        {
            var issues = CreateIssues();

            var cacheObject = CreateCacheObject(issues);

            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();

            var settingsSingleton = CreateSettingsSingleton(cacheObject);

            var testSubject = CreateTestSubject(fileStorage, settingsSingleton);
            var settings = testSubject.GetSettings("projectKey");

            fileStorage.Verify(fs => fs.Get(It.IsAny<string>()), Times.Never);
            settings.Should().BeSameAs(issues);
        }

        [TestMethod]
        public void GetSettings_SettingNotInCacheAndFile_SettingsEmpty()
        {
            var fileStorage = new Mock<ISuppressedIssuesFileStorage>();

            var testSubject = CreateTestSubject(fileStorage);
            var settings = testSubject.GetSettings("projectKey");

            fileStorage.Verify(fs => fs.Get("projectKey"), Times.Once);
            settings.Count().Should().Be(0);
        }

        [TestMethod]
        public void Invalidate_SettingInCache_SettingsRemovedFromCache()
        {
            var issues = CreateIssues();

            var cacheObject = CreateCacheObject(issues);

            var settingsSingleton = CreateSettingsSingleton(cacheObject);

            var testSubject = CreateTestSubject(settingsSingleton: settingsSingleton);
            testSubject.Invalidate("projectKey");

            cacheObject.ContainsKey("projectKey").Should().BeFalse();
        }
        [TestMethod]
        public void Invalidate_SettingNotInCache_NoErrorThrown()
        {
            var testSubject = CreateTestSubject();

            testSubject.Invalidate("projectKey");
        }

        private static ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> CreateCacheObject(IEnumerable<SonarQubeIssue> issues = null)
        {            
            var cacheObject = new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>();
            if (issues != null)
            {
                cacheObject.AddOrUpdate("projectKey", issues, (x, y) => issues);
            }
            return cacheObject;
        }

        private Mock<ISettingsSingleton> CreateSettingsSingleton(ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> cacheObject = null)
        {
            cacheObject = cacheObject ?? new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>();
            var settingsSingleton = new Mock<ISettingsSingleton>();
            settingsSingleton.SetupGet(s => s.Instance).Returns(cacheObject);
            return settingsSingleton;
        }

        private IEnumerable<SonarQubeIssue> CreateIssues()
        {
            SonarQubeIssue issue1 = TestHelper.CreateIssue("issueKey1");
            SonarQubeIssue issue2 = TestHelper.CreateIssue("issueKey2");
            return new List<SonarQubeIssue> { issue1, issue2 };
        }

        private SettingsCache CreateTestSubject(Mock<ISuppressedIssuesFileStorage> fileStorage = null, Mock<ISettingsSingleton> settingsSingleton = null)
        {
            fileStorage = fileStorage ?? new Mock<ISuppressedIssuesFileStorage>();
            settingsSingleton = settingsSingleton ?? CreateSettingsSingleton();

            return new SettingsCache(fileStorage.Object, settingsSingleton.Object);
        }
    }
}
