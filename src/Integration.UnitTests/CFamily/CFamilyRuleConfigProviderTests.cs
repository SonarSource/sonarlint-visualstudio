/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.CFamily;
using SonarLint.VisualStudio.Integration.NewConnectedMode;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyRuleConfigProviderTests
    {
        [TestMethod]
        public void Ctor()
        {
            var configProvider = new ConfigurableConfigurationProvider();
            var userSettingsProviderMock = new Mock<IUserSettingsProvider>();
            var sonarWayProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            var host = new Mock<IHost>();
            host.Setup(x => x.GetService(typeof(IConfigurationProvider))).Returns(configProvider);

            var testSubject = new CFamilyRuleConfigProvider(host.Object, userSettingsProviderMock.Object, new TestLogger(),
                sonarWayProviderMock.Object);

            // Assert
            host.Verify(x => x.GetService(typeof(IConfigurationProvider)), Times.Once);
        }
    }
}
