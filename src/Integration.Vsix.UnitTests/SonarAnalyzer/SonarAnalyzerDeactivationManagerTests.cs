//-----------------------------------------------------------------------
// <copyright file="SonarAnalyzerDeactivationManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    [TestClass]
    public class SonarAnalyzerDeactivationManagerTests
    {
        [TestMethod]
        public void SonarAnalyzerDeactivationManager_HasNoCollidingAnalyzerReference_OnEmptyList()
        {
            Assert.IsFalse(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(null)),
                "Null analyzer reference list should not report conflicting analyzer packages");

            Assert.IsFalse(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(new List<AnalyzerReference>())),
                "Empty analyzer reference list should not report conflicting analyzer packages");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_HasCollidingAnalyzerReference()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerDeactivationManager.AnalyzerVersion, version,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, version),
                    SonarAnalyzerDeactivationManager.AnalyzerName)
            };

            Assert.IsTrue(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "Conflicting analyzer package not found");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_HasNoCollidingAnalyzerReference_SameNameVersion()
        {
            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, SonarAnalyzerDeactivationManager.AnalyzerVersion),
                    SonarAnalyzerDeactivationManager.AnalyzerName)
            };

            Assert.IsFalse(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "Same named and versioned analyzers should not be reported as conflicting ones");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_HasNoCollidingAnalyzerReference_SameVersionDifferentName()
        {
            var name = "Some test name";
            Assert.AreNotEqual(SonarAnalyzerDeactivationManager.AnalyzerName, name,
                "Test input should be different from the expected analyzer name");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(name, SonarAnalyzerDeactivationManager.AnalyzerVersion), name)
            };

            Assert.IsFalse(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "Name is not considered in the confliction checking");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_HasNoCollidingAnalyzerReference_NoDisplayName()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerDeactivationManager.AnalyzerVersion, version,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, version),
                    null)
            };

            Assert.IsFalse(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "Null analyzer name should not report conflict");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_HasNoCollidingAnalyzerReference_NoAssemblyIdentity()
        {
            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new object(),
                    SonarAnalyzerDeactivationManager.AnalyzerName)
            };

            Assert.IsTrue(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "If no AssemblyIdentity is present, but the name matches, we should report a conflict");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_MultipleReferencesWithSameName_CollidingVersion()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerDeactivationManager.AnalyzerVersion, version,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, version),
                    SonarAnalyzerDeactivationManager.AnalyzerName),
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, SonarAnalyzerDeactivationManager.AnalyzerVersion),
                    SonarAnalyzerDeactivationManager.AnalyzerName),
            };

            Assert.IsFalse(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "Having already colliding references should not disable the embedded analyzer if one is of the same version");
        }

        [TestMethod]
        public void SonarAnalyzerDeactivationManager_MultipleReferencesWithSameName_NonCollidingVersion()
        {
            var version1 = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerDeactivationManager.AnalyzerVersion, version1,
                "Test input should be different from the expected analyzer version");
            var version2 = new Version("1.2.3.4");
            Assert.AreNotEqual(SonarAnalyzerDeactivationManager.AnalyzerVersion, version2,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, version1),
                    SonarAnalyzerDeactivationManager.AnalyzerName),
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerDeactivationManager.AnalyzerName, version2),
                    SonarAnalyzerDeactivationManager.AnalyzerName),
            };

            Assert.IsTrue(
                SonarAnalyzerDeactivationManager.HasConflictingAnalyzerReference(
                    SonarAnalyzerDeactivationManager.GetProjectAnalyzerConflictStatus(references)),
                "Having only different reference versions should disable the embedded analyzer");
        }
    }
}
