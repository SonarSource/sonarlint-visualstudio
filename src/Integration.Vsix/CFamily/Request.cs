/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Core.CFamily;
using static SonarLint.VisualStudio.Integration.Vsix.CFamily.CFamilyHelper;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    // Most of the Request class is ported from Java - see PortedFromJava\Request.cs
    internal partial class Request : IRequest
    {
        public string CFamilyLanguage { get; set; }

        public ICFamilyRulesConfig RulesConfiguration { get; set; }

        public CFamilyAnalyzerOptions AnalyzerOptions { get; set; }

        internal FileConfig FileConfig { get; set; }

        public void WriteRequest(BinaryWriter writer) =>
            Protocol.Write(writer, this);

        public void WriteRequestDiagnostics(TextWriter writer)
        {
            var serializedFileConfig = JsonConvert.SerializeObject(FileConfig);
            writer.Write(serializedFileConfig);
        }
    }
}
