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

using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Progress.MVVM;
using SonarLint.VisualStudio.Progress.Observation.ViewModels;

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