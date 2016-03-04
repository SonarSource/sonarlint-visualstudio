//-----------------------------------------------------------------------
// <copyright file="SonarAnalyzerCollisionManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SonarAnalyzerCollisionManagerTests
    {
        #region Tests

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_OnEmptyList()
        {
            Assert.IsFalse(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(null),
                "Null analyzer reference list should not report conflicting analyzer packages");

            Assert.IsFalse(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(new List<AnalyzerReference>()),
                "Empty analyzer reference list should not report conflicting analyzer packages");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasCollidingAnalyzerReference()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version),
                    SonarAnalyzerCollisionManager.AnalyzerName)
            };

            Assert.IsTrue(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "Conflicting analyzer package not found");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_SameNameVersion()
        {
            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, SonarAnalyzerCollisionManager.AnalyzerVersion),
                    SonarAnalyzerCollisionManager.AnalyzerName)
            };

            Assert.IsFalse(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "Same named and versioned analyzers should not be reported as conflicting ones");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_SameVersionDifferentName()
        {
            var name = "Some test name";
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerName, name,
                "Test input should be different from the expected analyzer name");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(name, SonarAnalyzerCollisionManager.AnalyzerVersion), name)
            };

            Assert.IsFalse(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "Name is not considered in the confliction checking");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_NoDisplayName()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version),
                    null)
            };

            Assert.IsFalse(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "Null analyzer name should not report conflict");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_NoAssemblyIdentity()
        {
            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new object(),
                    SonarAnalyzerCollisionManager.AnalyzerName)
            };

            Assert.IsTrue(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "If no AssemblyIdentity is present, but the name matches, we should report a conflict");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_MultipleReferencesWithSameName_CollidingVersion()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version),
                    SonarAnalyzerCollisionManager.AnalyzerName),
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, SonarAnalyzerCollisionManager.AnalyzerVersion),
                    SonarAnalyzerCollisionManager.AnalyzerName),
            };

            Assert.IsFalse(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "Having already colliding references should not disable the embedded analyzer if one is of the same version");
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_MultipleReferencesWithSameName_NonCollidingVersion()
        {
            var version1 = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version1,
                "Test input should be different from the expected analyzer version");
            var version2 = new Version("1.2.3.4");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version2,
                "Test input should be different from the expected analyzer version");

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version1),
                    SonarAnalyzerCollisionManager.AnalyzerName),
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version2),
                    SonarAnalyzerCollisionManager.AnalyzerName),
            };

            Assert.IsTrue(
                SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references),
                "Having only different reference versions should disable the embedded analyzer");
        }

        #endregion

        #region Helper AnalyzerReference class

        private class TestAnalyzerReference : AnalyzerReference
        {
            private readonly string displayName;
            private readonly object id;

            public TestAnalyzerReference(object id, string displayName)
            {
                this.id = id;
                this.displayName = displayName;
            }

            public override string Display
            {
                get
                {
                    return displayName;
                }
            }

            public override string FullPath
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override object Id
            {
                get
                {
                    return id;
                }
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            {
                throw new NotImplementedException();
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
