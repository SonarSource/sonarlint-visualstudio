//-----------------------------------------------------------------------
// <copyright file="ProgressControllerViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Progress.MVVM;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    [TestClass]
    public class ProgressControllerViewModelTests
    {
        [TestMethod]
        [Description("Verifies that all the publicly settable properties in ProgressControllerViewModel notify changes")]
        public void ProgressControllerViewModel_AllPublicPropertiesNotifyChanges()
        {
            ProgressControllerViewModel model = new ProgressControllerViewModel();
            ProgressStepViewModel step = new ProgressStepViewModel();
            model.Steps.Add(step);

            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, string>(model, "Title", "value1", "value2");
            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, ProgressStepViewModel>(model, "Current", null, step);
            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, bool>(model, "Cancellable", true, false);
            ViewModelVerifier.RunVerificationTest<ProgressControllerViewModel, ICommand>(model, "CancelCommand", null, new RelayCommand((s) => { }));
        }
    }
}
