using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands
{
    internal class AboutCommand : VsCommandBase
    {
        private readonly string aboutUrl = "https://marketplace.visualstudio.com/items?itemName=SonarSource.sonarlint-vscode";

        private readonly IBrowserService browserService;

        public AboutCommand(IBrowserService browserService)
        {
            this.browserService = browserService;
        }

        protected override void InvokeInternal()
        {
            browserService.Navigate(aboutUrl);
        }
    }
}
