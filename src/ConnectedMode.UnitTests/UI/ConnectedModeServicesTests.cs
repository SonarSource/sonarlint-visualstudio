/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class ConnectedModeServicesTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeServices, IConnectedModeServices>(
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ISlCoreConnectionAdapter>(),
            MefTestHelpers.CreateExport<IConfigurationProvider>(),
            MefTestHelpers.CreateExport<IServerConnectionsRepositoryAdapter>(),
            MefTestHelpers.CreateExport<ITelemetryManager>(),
            MefTestHelpers.CreateExport<ILogger>()
        );

    [TestMethod]
    public void Ctor_SetsLogContext()
    {
        var logger = Substitute.For<ILogger>();
        _ = new ConnectedModeServices(
            Substitute.For<IThreadHandling>(),
            Substitute.For<ISlCoreConnectionAdapter>(),
            Substitute.For<IConfigurationProvider>(),
            Substitute.For<IServerConnectionsRepositoryAdapter>(),
            logger,
            Substitute.For<ITelemetryManager>()
        );

        logger.Received().ForContext(Resources.ConnectedModeLogContext);
    }
}
