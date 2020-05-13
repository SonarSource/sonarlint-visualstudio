/*
 * SonarQube Client
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.UnitTests.Binding
{
    [TestClass]
    public class CompositeBindingConfigProviderTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            // 1. No config providers supplied
            Action act = () => new CompositeBindingConfigProvider();
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("providers");

            // 2. Null provider supplied
            var providerMock = new Mock<IBindingConfigProvider>();
            act = () => new CompositeBindingConfigProvider(providerMock.Object, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("providers");
        }


        [TestMethod]
        public void Ctor_ValidArgs()
        {
            // Arrange
            var providerMock1 = new Mock<IBindingConfigProvider>();
            var providerMock2 = new Mock<IBindingConfigProvider>();

            // Act
            var testSubject = new CompositeBindingConfigProvider(
                providerMock1.Object,
                providerMock2.Object, providerMock2.Object); // duplicate should be ignored

            // Assert
            testSubject.Providers.Count().Should().Be(2);
            testSubject.Providers.Should().BeEquivalentTo(providerMock1.Object, providerMock2.Object);
        }

        [TestMethod]
        public void IsSupported_ReturnsTrueIfSupportedByAny()
        {
            // Arrange
            var p1 = new DummyProvider(Language.C);
            var p2 = new DummyProvider(Language.VBNET);
            var p3 = new DummyProvider(Language.CSharp);

            var testSubject = new CompositeBindingConfigProvider(p1, p2, p3);

            // 1. Supported languages
            testSubject.IsLanguageSupported(Language.C).Should().BeTrue();
            testSubject.IsLanguageSupported(Language.VBNET).Should().BeTrue();
            testSubject.IsLanguageSupported(Language.CSharp).Should().BeTrue();

            // 2. Unsupported langauge
            testSubject.IsLanguageSupported(Language.Cpp).Should().BeFalse();
        }

        [TestMethod]
        public async Task GetConfiguration_WithMatchingProvider_ExpectedConfigReturned()
        {
            // Arrange
            var otherProvider = new DummyProvider(Language.VBNET);
            var cppProvider1 = new DummyProvider(Language.Cpp);
            var cppProvider2 = new DummyProvider(Language.Cpp);

            var qp = new SonarQubeQualityProfile("key", "name", "language", false, DateTime.UtcNow);

            var testSubject = new CompositeBindingConfigProvider(otherProvider, cppProvider1, cppProvider2);

            // Act. Multiple matching providers -> config from the first matching provider returned
            var actualConfig = await testSubject.GetConfigurationAsync(qp, Language.Cpp, BindingConfiguration.Standalone, CancellationToken.None);
            actualConfig.Should().Be(cppProvider1.ConfigToReturn);
        }

        [TestMethod]
        public void GetConfiguration_NoMatchingProvider_Throws()
        {
            // Arrange
            var otherProvider = new DummyProvider(Language.VBNET);
            var qp = new SonarQubeQualityProfile("key", "name", "language", false, DateTime.UtcNow);

            var testSubject = new CompositeBindingConfigProvider(otherProvider);

            // 1. Multiple matching providers -> config from the first matching provider returned
            Action act = () => testSubject.GetConfigurationAsync(qp, Language.Cpp, BindingConfiguration.Standalone, CancellationToken.None).Wait();

            act.Should().ThrowExactly<AggregateException>().And.InnerException.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        private class DummyProvider : IBindingConfigProvider
        {
            public DummyProvider(params Language[] supportedLanguages)
                : this(new Mock<IBindingConfig>().Object, supportedLanguages)
            {
            }

            public DummyProvider(IBindingConfig configToReturn = null, params Language[] supportedLanguages)
            {
                SupportedLanguages = new List<Language>(supportedLanguages);
                this.ConfigToReturn = configToReturn;
            }

            public IList<Language> SupportedLanguages { get; }

            public IBindingConfig ConfigToReturn { get; set; }

            #region IBindingConfigProvider implementation

            public Task<IBindingConfig> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language, BindingConfiguration bindingConfiguration, CancellationToken cancellationToken)
            {
                return Task.FromResult(ConfigToReturn);
            }

            public bool IsLanguageSupported(Language language)
            {
                return SupportedLanguages.Any(sl => sl.Equals(language));
            }

            #endregion IBindingConfigProvider implementation
        }
    }
}
