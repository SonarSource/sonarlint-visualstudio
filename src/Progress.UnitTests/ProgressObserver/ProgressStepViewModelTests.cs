//-----------------------------------------------------------------------
// <copyright file="ProgressStepViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.UnitTests;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class ProgressStepViewModelTests
    {
        [TestMethod]
        [Description("Verifies that all the publicly settable properties in ProgressStepViewModel notify changes")]
        public void ProgressStepViewModel_AllPublicPropertiesNotifyChanges()
        {
            ProgressStepViewModel model = new ProgressStepViewModel();

            ViewModelVerifier.RunVerificationTest<ProgressStepViewModel, string>(model, "DisplayText", "value1", "value2");
            ViewModelVerifier.RunVerificationTest<ProgressStepViewModel, StepExecutionState>(model, "ExecutionState", StepExecutionState.Cancelled, StepExecutionState.Failed);
            ViewModelVerifier.RunVerificationTest<ProgressStepViewModel, string>(model, "ProgressDetailText", null, string.Empty);
        }
    }
}
