﻿/*
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

using System;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.QualityProfiles;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BindingProcessFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BindingProcessFactory, IBindingProcessFactory>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IExclusionSettingsStorage>(),
                MefTestHelpers.CreateExport<IQualityProfileDownloader>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Create_ReturnsProcessImpl()
        {
            var bindingArgs = new BindCommandArgs(new BoundServerProject("any", "any", new ServerConnection.SonarCloud("any")));

            var testSubject = CreateTestSubject();

            var actual = testSubject.Create(bindingArgs);
            actual.Should().Should().NotBeNull();
            actual.Should().BeOfType<BindingProcessImpl>();
        }

        private static BindingProcessFactory CreateTestSubject(
            ISonarQubeService service = null,
            IExclusionSettingsStorage exclusionSettingsStorage = null,
            IQualityProfileDownloader qualityProfileDownloader = null,
            ILogger logger = null)
        {
            service ??= Mock.Of<ISonarQubeService>();
            exclusionSettingsStorage ??= Mock.Of<IExclusionSettingsStorage>();
            qualityProfileDownloader ??= Mock.Of<IQualityProfileDownloader>();
            logger ??= new TestLogger(logToConsole: true);

            return new BindingProcessFactory(service, exclusionSettingsStorage, qualityProfileDownloader, logger);
        }

    }
}
