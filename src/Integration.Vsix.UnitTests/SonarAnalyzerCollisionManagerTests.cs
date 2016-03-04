//-----------------------------------------------------------------------
// <copyright file="BoundSolutionAnalyzerTests.cs" company="SonarSource SA and Microsoft Corporation">
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
            var hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(null);
            Assert.IsFalse(hasConflict);

            hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(new List<AnalyzerReference>());
            Assert.IsFalse(hasConflict);
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasCollidingAnalyzerReference()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version);

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version),
                    SonarAnalyzerCollisionManager.AnalyzerName)
            };

            var hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references);
            Assert.IsTrue(hasConflict);
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

            var hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references);
            Assert.IsFalse(hasConflict);
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_SameVersionDifferentName()
        {
            var name = "Some test name";
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerName, name);

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(name, SonarAnalyzerCollisionManager.AnalyzerVersion), name)
            };

            var hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references);
            Assert.IsFalse(hasConflict);
        }

        [TestMethod]
        public void SonarAnalyzerCollisionManager_HasNoCollidingAnalyzerReference_NoDisplayName()
        {
            var version = new Version("0.1.2.3");
            Assert.AreNotEqual(SonarAnalyzerCollisionManager.AnalyzerVersion, version);

            IEnumerable<AnalyzerReference> references = new AnalyzerReference[]
            {
                new TestAnalyzerReference(
                    new AssemblyIdentity(SonarAnalyzerCollisionManager.AnalyzerName, version),
                    null)
            };

            var hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references);
            Assert.IsFalse(hasConflict);
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

            var hasConflict = SonarAnalyzerCollisionManager.HasConflictingAnalyzerReference(references);
            Assert.IsTrue(hasConflict);
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
