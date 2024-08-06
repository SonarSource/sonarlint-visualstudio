using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.Migration.Wizard;
using MessageBox = SonarLint.VisualStudio.Core.MessageBox;

namespace SonarLint.VisualStudio.Integration.Vsix.Commands.ConnectedModeMenu
{
    public class NewConnectedModeCommand : VsCommandBase
    {
        internal const int Id = 0x104;

        protected override void InvokeInternal()
        {
           new NewConnectedMode().ShowDialog();
        }
    }
}
