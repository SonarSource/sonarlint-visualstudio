/*
 * SonarLint for Visual Studio
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

using FluentAssertions;
using SonarLint.VisualStudio.Integration.Vsix;
using System;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class DummyDaemonInstaller : IDaemonInstaller
    {
        #region Test helpers

        public int InstallCallCount { get; private set; }

        public bool IsInstalledReturnValue { get; set; }

        public void AssertNoEventHandlersRegistered()
        {
            this.InstallationProgressChanged.Should().BeNull();
            this.InstallationCompleted.Should().BeNull();
        }

        public void SimulateProgressChanged(InstallationProgressChangedEventArgs args)
        {
            this.InstallationProgressChanged?.Invoke(this, args);
        }

        public void SimulateInstallFinished(AsyncCompletedEventArgs args)
        {
            this.IsInstalledReturnValue = true;
            this.InstallationCompleted?.Invoke(this, args);
        }

        #endregion

        #region IDaemonInstaller methods

        public bool InstallInProgress { get; set; } /* publicly settable for testing */

        public string InstallationPath => throw new NotImplementedException();

        public string DaemonVersion => throw new NotImplementedException();

        public event InstallationProgressChangedEventHandler InstallationProgressChanged;
        public event AsyncCompletedEventHandler InstallationCompleted;

        public void Install()
        {
            InstallCallCount++;
        }

        public bool IsInstalled()
        {
            return IsInstalledReturnValue;
        }

        #endregion
    }
}
