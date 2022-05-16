using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging.Adornment
{
    internal class ErrorLayer
    {
        public const string LayerName = "SonarLint error adornments";

        [Export(typeof(AdornmentLayerDefinition))]
        [Name(LayerName)]
        [Order(After = PredefinedAdornmentLayers.Caret)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;
    }
}
