//-----------------------------------------------------------------------
// <copyright file="ConfigurableProgressController.IProgressController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Controller.ErrorNotification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

        Task<ProgressControllerResult> IProgressController.Start()
        {
            return Task.Factory.StartNew(() =>
                {
                    stepOperations.ForEach(op =>
                    {
                        try
                        {
                            this.canAbort = op.Step.Cancellable;
                            StepExecutionState state = op.Run(this.cts.Token, this).Result;
                            VerificationHelper.CheckState(op.Step, state);
                        }
                        catch (Exception ex)
                        {
                            Assert.Fail(ex.ToString());
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
