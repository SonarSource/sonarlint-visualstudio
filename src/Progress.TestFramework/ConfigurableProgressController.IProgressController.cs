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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial class implementation of <see cref="IProgressController"/>
    /// </summary>
    public partial class ConfigurableProgressController : IProgressController
    {
        IProgressEvents IProgressController.Events
        {
            get { return this.Events; }
        }

        IErrorNotificationManager IProgressController.ErrorNotificationManager
        {
            get { return this.ErrorNotificationManager; }
        }

        Task<ProgressControllerResult> IProgressController.StartAsync()
        {
            return Task.Factory.StartNew(() =>
                {
                    stepOperations.ForEach(op =>
                    {
                        try
                        {
                            this.canAbort = op.Step.Cancellable;
                            StepExecutionState state = op.RunAsync(this.cts.Token, this).Result;
                            VerificationHelper.CheckState(op.Step, state);
                        }
                        catch (Exception ex)
                        {
                            FluentAssertions.Execution.Execute.Assertion.FailWith(ex.ToString());
                        }
                    });
                    return this.ReturnResult;
                });
        }

        void IProgressController.Initialize(IProgressStepFactory stepFactory, IEnumerable<IProgressStepDefinition> stepsDefinition)
        {
            throw new NotImplementedException();
        }

        bool IProgressController.TryAbort()
        {
            if (this.TryAbortAction != null)
            {
                return this.TryAbortAction();
            }

            throw new NotImplementedException();
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            if (this.ServiceProvider != null)
            {
                return this.ServiceProvider.GetService(serviceType);
            }

            throw new NotImplementedException();
        }
    }
}