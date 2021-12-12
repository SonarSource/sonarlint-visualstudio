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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    internal class CFamilyIssueConverterFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CFamilyIssueConverterFactory, ICFamilyIssueConverterFactory>(null, new[]
            {
                MefTestHelpers.CreateExport<ITextDocumentFactoryService>(Mock.Of<ITextDocumentFactoryService>()),
                MefTestHelpers.CreateExport<IContentTypeRegistryService>(Mock.Of<IContentTypeRegistryService>())
            });
        }

        [TestMethod]
        public void Create_ReturnsNewInstance()
        {
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>();
            var contentTypeRegistry = Mock.Of<IContentTypeRegistryService>();

            var testSubject = new CFamilyIssueConverterFactory(textDocumentFactory, contentTypeRegistry);

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
