using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.Settings.Cache
{
    [TestClass]
    public class SettingsSingletonTests
    {
        [TestMethod]
        public void Instance_ShouldBeSameInMultipleClassInstances()
        {
            var instance1 = new SettingsSingleton();
            var instance2 = new SettingsSingleton();

            instance1.Instance.Should().BeSameAs(instance2.Instance);
        }
    }
}
