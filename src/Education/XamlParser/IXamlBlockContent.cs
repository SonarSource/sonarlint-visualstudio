using System.Windows.Documents;

namespace SonarLint.VisualStudio.Education.XamlParser
{

    /// <summary>
    /// Represents a XAML entity that is parseable into Block object
    /// </summary>
    internal interface IXamlBlockContent
    {
        Block GetObjectRepresentation();
    }

    internal class GeneratedXamlBlockContent : IXamlBlockContent
    {
        public Block GetObjectRepresentation()
        {
            // todo XamlReader.Parse(xamlString)
            throw new System.NotImplementedException();
        }
    }

    internal class StaticXamlBlockContent : IXamlBlockContent
    {
        public Block GetObjectRepresentation()
        {
            // todo something like XamlReader.Load(someXamlResource)
            throw new System.NotImplementedException();
        }
    }
}
