/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarAnalyzer
{
    internal class ConfigurableAnalyzerReference : AnalyzerReference
    {
        private readonly string displayName;
        private readonly object id;

        public ConfigurableAnalyzerReference(object id, string displayName)
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