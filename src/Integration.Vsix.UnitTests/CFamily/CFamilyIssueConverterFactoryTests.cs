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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyIssueConverterFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CFamilyIssueConverterFactory, ICFamilyIssueConverterFactory>
                (MefTestHelpers.CreateExport<ITextDocumentFactoryService>(Mock.Of<ITextDocumentFactoryService>()),
                MefTestHelpers.CreateExport<IContentTypeRegistryService>(),
                MefTestHelpers.CreateExport<IConnectedModeFeaturesConfiguration>());
        }

        [TestMethod]
        public void Create_ReturnsNewInstance()
        {
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>();
            var contentTypeRegistry = Mock.Of<IContentTypeRegistryService>();
            var connectedModeFeaturesConfiguration = Mock.Of<IConnectedModeFeaturesConfiguration>();

            var testSubject = new CFamilyIssueConverterFactory(textDocumentFactory, contentTypeRegistry, connectedModeFeaturesConfiguration);

            // Create first item
            var result1 = testSubject.Create();
            result1.Should().NotBeNull();

            // Create second item
            var result2 = testSubject.Create();
            result2.Should().NotBeNull();

            result1.Should().NotBeSameAs(result2);
        }
    }
}
