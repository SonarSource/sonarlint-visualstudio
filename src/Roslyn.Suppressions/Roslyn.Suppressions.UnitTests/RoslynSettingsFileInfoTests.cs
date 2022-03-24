using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class RoslynSettingsFileInfoTests
    {

        [DataRow("projectkey", "projectkey")]//Testing lower case
        [DataRow("projectKEY", "projectkey")]//Testing upper case
        [DataRow("III", "iii")]//Testing upper case with invariant culture
        [DataRow("project:key", "project_key")]//Testing illegal characters
        [TestMethod]
        public void GetSettingsFilePath_ReturnsFilePathCorrectly(string projectKey, string expectedFileName)
        {
            var expectedPath = Path.Combine(RoslynSettingsFileInfo.Directory, expectedFileName + ".json");

            //This is to make sure normalising the keys done correctly with invariant culture
            //https://en.wikipedia.org/wiki/Dotted_and_dotless_I 
            using (new TemporaryCultureSwitch(new CultureInfo("tr-TR")))
            {
                var actualPath = RoslynSettingsFileInfo.GetSettingsFilePath(projectKey);

                actualPath.Should().Be(expectedPath);
            }
        }
    }
}
