/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SanityTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableTelemetryLogger logger;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.logger = new ConfigurableTelemetryLogger();
            this.serviceProvider.RegisterService(typeof(SComponentModel),
                ConfigurableComponentModel.CreateWithExports(
                    MefTestHelpers.CreateExport<ITelemetryLogger>(this.logger)));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.logger.DumpAllToOutput();
        }

        private static void RetryAction(Func<bool> action, string description, int maxAttempts = 3)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (!action())
                {
                    Thread.Sleep(100);
                }
                else
                {
                    return;
                }
            }

            FluentAssertions.Execution.Execute.Assertion.FailWith("Failed executing the action (with retries): {0}", description);
        }
    }
}