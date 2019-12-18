/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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

using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    // Most of the Request class is ported from Java - see PortedFromJava\Request.cs
    // This partial contains additional properties that don't appear in the Java version
    // and aren't passed as part of the request to the CLang analyzer, but are used by SLVS
    // when filtering the returned issues.
    internal partial class Request
    {
        // Note: the language and RulesConfiguration aren't passed as part of the request to the
        // CLang analyzer, but it is by SVLS used when filtering the returned issues.
        public string CFamilyLanguage { get; set; }

        public ICFamilyRulesConfig RulesConfiguration { get; set; }
    }
}
