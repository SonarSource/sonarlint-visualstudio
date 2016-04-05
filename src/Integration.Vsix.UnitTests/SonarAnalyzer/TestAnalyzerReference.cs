//-----------------------------------------------------------------------
// <copyright file="TestAnalyzerReference.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    internal class TestAnalyzerReference : AnalyzerReference
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
}
