//-----------------------------------------------------------------------
// <copyright file="AnalyzerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Service;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.Service
{
    [TestClass]
    public class VersionHelperTests
    {
        [TestMethod]
        public void VersionHelper_Compare_NullVersionStrings_ThrowsException()
        {
            Exceptions.Expect<ArgumentNullException>(() => VersionHelper.Compare(null, "1.2.3"));
            Exceptions.Expect<ArgumentNullException>(() => VersionHelper.Compare("1.2.3", null));
        }

        [TestMethod]
        public void VersionHelper_Compare_InvalidVersionStrings_ThrowsException()
        {
            Exceptions.Expect<ArgumentException>(() => VersionHelper.Compare("notaversion", "1.2.3"));
            Exceptions.Expect<ArgumentException>(() => VersionHelper.Compare("1.2.3", "notaversion"));
        }

        [TestMethod]
        public void VersionHelper_Compare_SameVersionString_Release_AreSame()
        {
            // Act
            int result = VersionHelper.Compare("1.2.3", "1.2.3");

            // Verify
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void VersionHelper_Compare_SameVersionString_Prerelease_AreSame()
        {
            // Test case 1: same 'dev string'
            // Act
            int result1 = VersionHelper.Compare("1.0-rc1", "1.0-rc2");

            // Verify
            Assert.AreEqual(0, result1);
        }

        [TestMethod]
        public void VersionHelper_Compare_ReleaseAndPrerelease_ComparesOnlyNumericParts()
        {
            // Act + Verify
            Assert.IsTrue(VersionHelper.Compare("1.1", "1.2-beta") < 0);
            Assert.IsTrue(VersionHelper.Compare("1.1-beta", "1.2") < 0);
        }

        [TestMethod]
        public void VersionHelper_Compare_NextMinorVersion()
        {
            // Act + Verify
            Assert.IsTrue(VersionHelper.Compare("1.2", "1.3") < 0);
            Assert.IsTrue(VersionHelper.Compare("1.3", "1.2") > 0);
        }
    }
}
