using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.Commands;
using SonarLint.VisualStudio.Core;
using System.Windows.Navigation;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands.HelpMenu
{
    [TestClass]
    internal class AboutDialogTests
    {
        [TestMethod]
        public void Invoke_VerifyCallsNavigateOnce()
        {
            var eventArgs = new Mock<RequestNavigateEventArgs>();
            string path = "test";
            eventArgs.Setup(x => x.Uri.AbsoluteUri).Returns(path);

            var browserService = new Mock<IBrowserService>();

            var testSubject = new AboutDialog(browserService.Object);

            browserService.Verify(x => x.Navigate(path), Times.Never);
            testSubject.ViewWebsite(It.IsAny<object>(), eventArgs.Object);
            browserService.Verify(x => x.Navigate(path), Times.Once);
        }
    }
}
