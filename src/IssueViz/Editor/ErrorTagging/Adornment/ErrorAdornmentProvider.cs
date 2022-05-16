using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Adornment
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class ErrorAdornmentProvider : IWpfTextViewCreationListener
    {

        private const double _initOpacity = 0.6D;

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        #region IWpfTextViewCreationListener interface

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                string fileName = Path.GetFileName(document.FilePath).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileName)) { return; }

                var aboveAdornment = new ErrorsAboveBelowAdornment(textView, _initOpacity, true, "0 issues above");
                var bellowAdornment = new ErrorsAboveBelowAdornment(textView, _initOpacity, false, "4 issues below");
            }
        }

        #endregion IWpfTextViewCreationListener interface
    }
}
