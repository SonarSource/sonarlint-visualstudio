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

using System;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.NewConnectedMode
{
    internal class UnbindCommand
    {
        private readonly IHost host;
        private readonly IConfigurationProvider configProvider;

        public UnbindCommand(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            this.host = host;

            this.configProvider = this.host.GetService<IConfigurationProvider>();
            configProvider.AssertLocalServiceIsNotNull();
        }

        public bool CanExecute()
        {
            // Can only unbind if currently in new bound mode
            return  !host.VisualStateManager.IsBusy &&
                configProvider.GetConfiguration().Mode == SonarLintMode.Connected;
        }

        public void Execute()
        {
            Debug.Assert(this.CanExecute(), $"Should not be called UnbindCommand.Execute if CanExecute is false");
            Debug.Assert(ThreadHelper.CheckAccess(), "Expected step to be run on the UI thread");

            host.Logger.WriteLine(Strings.Unbind_State_Started);
            try
            {
                host.VisualStateManager.IsBusy = true;

                host.Logger.WriteLine(Strings.Unbind_DeletingBinding);
                configProvider.DeleteConfiguration();

                host.Logger.WriteLine(Strings.Unbind_DisconnectingFromSonarQube);
                host.ActiveSection.DisconnectCommand.Execute(null);

                host.VisualStateManager.ClearBoundProject(); // this will raise a "binding changed" event

                host.Logger.WriteLine(Strings.Unbind_State_Succeeded);
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                host.Logger.WriteLine(ex.Message);
                host.Logger.WriteLine(Strings.Unbind_State_Failed);
            }
            finally
            {
                host.VisualStateManager.IsBusy = false;
            }
        }
    }
}