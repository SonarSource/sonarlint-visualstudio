/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class BindingProcessFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BindingProcessFactory, IBindingProcessFactory>(
                MefTestHelpers.CreateExport<IHost>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IExclusionSettingsStorage>(),
                MefTestHelpers.CreateExport<IConfigurationPersister>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Create_ReturnsProcessImpl()
        {
            var bindingArgs = new BindCommandArgs("proj key", "proj name", new ConnectionInformation(new Uri("http://localhost")));

            var testSubject = CreateTestSubject();

            var actual = testSubject.Create(bindingArgs, false);
            actual.Should().Should().NotBeNull();
            actual.Should().BeOfType<BindingProcessImpl>();

            var actualImpl = (BindingProcessImpl)actual;
            actualImpl.InternalState.IsFirstBinding.Should().BeFalse();
        }

        private static BindingProcessFactory CreateTestSubject(IHost host = null,
            ISonarQubeService service = null,
            IExclusionSettingsStorage exclusionSettingsStorage = null,
            IConfigurationPersister configurationPersister = null,
            ILogger logger = null)
        {
            host ??= Mock.Of<IHost>();
            service ??= Mock.Of<ISonarQubeService>();
            exclusionSettingsStorage ??= Mock.Of<IExclusionSettingsStorage>();
            configurationPersister ??= Mock.Of<IConfigurationPersister>();
            logger ??= new TestLogger(logToConsole: true);

            return new BindingProcessFactory(host, service, exclusionSettingsStorage, configurationPersister, logger);
        }

    }
}
