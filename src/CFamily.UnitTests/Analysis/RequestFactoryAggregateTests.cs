﻿/*
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

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.CFamily.UnitTests.Analysis
{
    [TestClass]
    public class RequestFactoryAggregateTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            batch.AddExport(MefTestHelpers.CreateExport<IRequestFactory>(Mock.Of<IRequestFactory>()));
            batch.AddExport(MefTestHelpers.CreateExport<IRequestFactory>(Mock.Of<IRequestFactory>()));
            batch.AddExport(MefTestHelpers.CreateExport<IRequestFactory>(Mock.Of<IRequestFactory>()));

            var aggregateImport = new SingleObjectImporter<IRequestFactoryAggregate>();
            batch.AddPart(aggregateImport);

            using var catalog = new TypeCatalog(typeof(RequestFactoryAggregate));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            aggregateImport.Import.Should().NotBeNull();
        }

        [TestMethod]
        public void TryGet_NullFilePath_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.TryGet(null, new CFamilyAnalyzerOptions());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("analyzedFilePath");
        }

        [TestMethod]
        public void TryGet_NoFactories_Null()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.TryGet("path", new CFamilyAnalyzerOptions());

            result.Should().BeNull();
        }

        [TestMethod]
        public void TryGet_NoMatchingFactory_Null()
        {
            var factory1 = new Mock<IRequestFactory>();
            var factory2 = new Mock<IRequestFactory>();

            var testSubject = CreateTestSubject(factory1.Object, factory2.Object);

            var options = new CFamilyAnalyzerOptions();
            var result = testSubject.TryGet("path", options);

            result.Should().BeNull();

            factory1.Verify(x=> x.TryGet("path", options), Times.Once);
            factory2.Verify(x=> x.TryGet("path", options), Times.Once);
        }

        [TestMethod]
        public void TryGet_HasMatchingFactory_OtherFactoriesNotChecked()
        {
            var factory1 = new Mock<IRequestFactory>();
            var factory2 = new Mock<IRequestFactory>();
            var factory3 = new Mock<IRequestFactory>();

            var requestToReturn = Mock.Of<IRequest>();
            var options = new CFamilyAnalyzerOptions();
            factory2.Setup(x => x.TryGet("path", options)).Returns(requestToReturn);

            var testSubject = CreateTestSubject(factory1.Object, factory2.Object, factory3.Object);

            var result = testSubject.TryGet("path", options);

            result.Should().Be(requestToReturn);

            factory1.Verify(x => x.TryGet("path", options), Times.Once);
            factory2.Verify(x => x.TryGet("path", options), Times.Once);
            factory3.Invocations.Count.Should().Be(0);
        }

        private RequestFactoryAggregate CreateTestSubject(params IRequestFactory[] requestFactories) => 
            new RequestFactoryAggregate(requestFactories);
    }
}
