/*
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

using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.TaintList
{
    [TestClass]
    public class TaintToolWindowTests
    {
        [TestMethod]
        public void Ctor_CaptionIsSet()
        {
            var viewModel = CreateViewModel("initial");
            var testSubject = new TaintToolWindow(viewModel.Object);

            testSubject.Caption.Should().Be("initial");
        }

        [TestMethod]
        public void Ctor_RegistersToViewModelEvents()
        {
            var viewModel = CreateViewModel("any");
            var testSubject = new TaintToolWindow(viewModel.Object);

            viewModel.VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>());
        }

        [TestMethod]
        public void Dispose_UnregistersFromViewModelEvents()
        {
            var viewModel = CreateViewModel("any");
            var testSubject = new TaintToolWindow(viewModel.Object);

            testSubject.Dispose();

            viewModel.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void PropertyChanged_IsCaptionProperty_CaptionUpdated()
        {
            var viewModel = CreateViewModel("initial caption");
            var testSubject = new TaintToolWindow(viewModel.Object);

            SetCaption(viewModel, "new caption");
            RaisePropertyChanged(viewModel, "WindowCaption");

            testSubject.Caption.Should().Be("new caption");
        }

        [TestMethod]
        public void PropertyChanged_IsNotCaptionProperty_CaptionNotUpdated()
        {
            var viewModel = CreateViewModel("initial caption");
            var testSubject = new TaintToolWindow(viewModel.Object);

            SetCaption(viewModel, "new caption");
            RaisePropertyChanged(viewModel, "SomeOtherProperty");

            testSubject.Caption.Should().Be("initial caption");
        }

        private static Mock<ITaintIssuesControlViewModel> CreateViewModel(string caption)
        {
            var viewModel = new Mock<ITaintIssuesControlViewModel>();
            viewModel.SetupAdd(x => x.PropertyChanged += (sender, args) => { });

            SetCaption(viewModel, caption);
            return viewModel;
        }

        private static void SetCaption(Mock<ITaintIssuesControlViewModel> viewModel, string caption) =>
            viewModel.Setup(x => x.WindowCaption).Returns(caption);

        private static void RaisePropertyChanged(Mock<ITaintIssuesControlViewModel> viewModel, string propertyName) =>
            viewModel.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(propertyName));
    }
}
