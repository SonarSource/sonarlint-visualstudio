using System.ComponentModel.Composition;

namespace Payload
{

    internal class PathExporter
    {
        [Export("SonarLint.Payload.ExtensionPath")]
        public string ExtensionPath => System.IO.Path.GetDirectoryName(typeof(PathExporter).Assembly.Location);
    }
}
