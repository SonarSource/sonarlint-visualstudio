using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet;
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
        public void PropertyChanged_IsNotCaptionProperty_CaptionUpdated()
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
