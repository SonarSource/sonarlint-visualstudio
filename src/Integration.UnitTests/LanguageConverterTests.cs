//-----------------------------------------------------------------------
// <copyright file="LanguageConverterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LanguageConverterTests
    {
        #region Test boilerplate

        private LanguageConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            this.testSubject = new LanguageConverter();
        }

        #endregion

        #region Tests

        [TestMethod]
        public void LanguageConverter_CanConvertFrom_StringType_IsTrue()
        {
            // Act
            var canConvertFrom = this.testSubject.CanConvertFrom(typeof(string));

            // Verify
            Assert.IsTrue(canConvertFrom, "Expected to be able to convert from string");
        }

        [TestMethod]
        public void LanguageConverter_ConvertFrom_KnownLanguageId_ReturnsKnownLanguage()
        {
            // Act
            object result = this.testSubject.ConvertFrom(Language.VBNET.Id);

            // Verify
            Assert.IsInstanceOfType(result, typeof(Language));
            Assert.AreEqual(Language.VBNET, result);
        }

        [TestMethod]
        public void LanguageConverter_ConvertFrom_UnknownLanguageId_ReturnsUnknownLanguage()
        {
            // Act
            object result = this.testSubject.ConvertFrom("WhoAmI?");

            // Verify
            Assert.IsInstanceOfType(result, typeof(Language));
            Assert.AreEqual(Language.Unknown, result);
        }

        [TestMethod]
        public void LanguageConverter_ConvertFrom_Null_ReturnsUnknownLanguage()
        {
            // Act
            object result;
            using (new AssertIgnoreScope()) // null input
            {
                result = this.testSubject.ConvertFrom(null);
            }

            // Verify
            Assert.IsInstanceOfType(result, typeof(Language));
            Assert.AreEqual(Language.Unknown, result);
        }

        [TestMethod]
        public void LanguageConverter_CanConvertTo_StringType_IsTrue()
        {
            // Act
            var canConvertTo = this.testSubject.CanConvertTo(typeof(string));

            // Verify
            Assert.IsTrue(canConvertTo, "Expected to be able to convert to string");
        }

        [TestMethod]
        public void LanguageConverter_ConvertTo_Language_ReturnsLanguageId()
        {
            // Act
            object result = this.testSubject.ConvertTo(Language.VBNET, typeof(string));

            // Verify
            Assert.IsInstanceOfType(result, typeof(string));
            Assert.AreEqual(Language.VBNET.Id, result);
        }

        [TestMethod]
        public void LanguageConverter_ConvertTo_Null_ReturnsNull()
        {
            // Act
            object result;
            using (new AssertIgnoreScope()) // null input
            {
                result = this.testSubject.ConvertTo(null, typeof(string));
            }

            // Verify
            Assert.IsNull(result);
        }

        #endregion
    }
}
