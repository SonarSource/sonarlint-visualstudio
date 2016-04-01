using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LanguageTests
    {
        [TestMethod]
        public void Language_Ctor_ArgChecks()
        {
            // Setup
            var key = "k";
            var name = "MyName";
            var guid = Guid.NewGuid().ToString("N");

            // Act + Verify
            // Nulls
            Exceptions.Expect<ArgumentNullException>(() => new Language(null, name, guid));
            Exceptions.Expect<ArgumentNullException>(() => new Language(key, null, guid));
            Exceptions.Expect<ArgumentNullException>(() => new Language(key, name, null));

            // Bad GUID
            Exceptions.Expect<FormatException>(() => new Language(key, name, "thisisnotaguid"));
        }

        [TestMethod]
        public void Language_IsSupported_SupportedLanguage_IsTrue()
        {
            // Act + Verify
            foreach(var supportedLang in Language.SupportedLanguages)
            {
                Assert.IsTrue(supportedLang.IsSupported, "Supported langugage should be supported");
            }
        }

        [TestMethod]
        public void Language_IsSupported_UnsupportedLanguage_IsFalse()
        {
            // Setup
            var unsupportedLangs = Language.KnownLanguages.Except(Language.SupportedLanguages);

            // Sanity
            Debug.Assert(unsupportedLangs.Any(), "No known but unsupported languages");

            // Act + Verify
            foreach (var unsupportedLang in unsupportedLangs)
            {
                Assert.IsFalse(unsupportedLang.IsSupported, "Unsupported langugage should NOT be supported");
            }
        }

        [TestMethod]
        public void Language_ForProject_KnownLanguage_ReturnsCorrectLanguage()
        {
            // Test case 1: Unknown
            // Setup
            var otherProject = new ProjectMock("other.proj");

            // Act
            var otherProjectLanguage = Language.ForProject(otherProject);

            // Verify
            Assert.IsNull(otherProjectLanguage, "Expected Language to be null for unknown project");

            // Test case 2: C#
            // Setup
            var csProject = new ProjectMock("cs1.csproj");
            csProject.SetCSProjectKind();

            // Act
            var csProjectLanguage = Language.ForProject(csProject);

            // Verify
            Assert.AreEqual(Language.CSharp, csProjectLanguage, "Unexpected Language for C# project");

            // Test case 3: VB
            // Setup
            var vbNetProject = new ProjectMock("vb1.vbproj");
            vbNetProject.SetVBProjectKind();

            // Act
            var vbNetProjectLanguage = Language.ForProject(vbNetProject);

            // Verify
            Assert.AreEqual(Language.VBNET, vbNetProjectLanguage, "Unexpected Language for C# project");
        }

        [TestMethod]
        public void Language_Equality()
        {
            // Setup
            var lang1a = new Language("lang1", "Language 1", "{4FE75C7D-F43F-4A72-940C-47C97710BCCA}");
            var lang1b = new Language("lang1", "Language 1", "{4FE75C7D-F43F-4A72-940C-47C97710BCCA}");
            var lang2 = new Language("lang2", "Language 2", "{7A128822-05AA-49D0-A3C7-16F03F3A92E5}");

            // Act + Verify
            Assert.AreEqual(lang1a, lang1b, "Languages with the same keys and GUIDs should be equal");
            Assert.AreNotEqual(lang1a, lang2, "Languages with different keys and GUIDs should NOT be equal");
        }
    }
}
