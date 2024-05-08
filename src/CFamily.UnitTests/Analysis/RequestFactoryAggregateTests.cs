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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.CFamily.Analysis.UnitTests
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

            Func<Task> act = () => testSubject.TryCreateAsync(null, new CFamilyAnalyzerOptions());

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("analyzedFilePath");
        }

        [TestMethod]
        public async Task TryGet_NoFactories_Null()
        {
            var testSubject = CreateTestSubject();

            var result = await testSubject.TryCreateAsync("path", new CFamilyAnalyzerOptions());

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task TryGet_NoMatchingFactory_Null()
        {
            var factory1 = new Mock<IRequestFactory>();
            var factory2 = new Mock<IRequestFactory>();

            var testSubject = CreateTestSubject(factory1.Object, factory2.Object);

            var options = new CFamilyAnalyzerOptions();
            var result = await testSubject.TryCreateAsync("path", options);

            result.Should().BeNull();

            factory1.Verify(x=> x.TryCreateAsync("path", options), Times.Once);
            factory2.Verify(x=> x.TryCreateAsync("path", options), Times.Once);
        }

        [TestMethod]
        public async Task TryGet_HasMatchingFactory_OtherFactoriesNotChecked()
        {
            var factory1 = new Mock<IRequestFactory>();
            var factory2 = new Mock<IRequestFactory>();
            var factory3 = new Mock<IRequestFactory>();

            var requestToReturn = Mock.Of<IRequest>();
            var options = new CFamilyAnalyzerOptions();
            factory2.Setup(x => x.TryCreateAsync("path", options)).Returns(Task.FromResult(requestToReturn));

            var testSubject = CreateTestSubject(factory1.Object, factory2.Object, factory3.Object);

            var result = await testSubject.TryCreateAsync("path", options);

            result.Should().Be(requestToReturn);

            factory1.Verify(x => x.TryCreateAsync("path", options), Times.Once);
            factory2.Verify(x => x.TryCreateAsync("path", options), Times.Once);
            factory3.Invocations.Count.Should().Be(0);
        }

        private RequestFactoryAggregate CreateTestSubject(params IRequestFactory[] requestFactories) => 
            new RequestFactoryAggregate(requestFactories);
    }
}
