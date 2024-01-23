/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Composition.Hosting;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.AdditionalFiles.UnitTests
{
    [TestClass]
    public class TypeScriptPathsProviderTests
    {
        [TestMethod]
        [DataRow("SonarLint.TypeScript.EsLintBridgeServerPath", @"\ts\bin\server")]
        [DataRow("SonarLint.TypeScript.RuleDefinitionsFilePath", @"\ts\sonarlint-metadata.json")]
        public void ExportPathByContractName_ReturnsExpectedValue(string contractName, string expectedEnding)
        {
            using var scope = new AssertIgnoreScope();

            var exportedValue = GetExportedString(contractName);

            exportedValue.Should().EndWith(expectedEnding);
            Path.IsPathRooted(exportedValue).Should().BeTrue();
        }

        private static string GetExportedString(string contractName)
        {
            var catalog = new TypeCatalog(typeof(TypeScriptPathsProvider));
            using var container = new CompositionContainer(catalog);
            return container.GetExportedValue<string>(contractName);
        }
    }
}
