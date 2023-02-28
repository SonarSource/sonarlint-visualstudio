using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SonarLint.VisualStudio.Education.XamlGenerator
{
    internal static class XmlWriterExtensions
    {
        internal static void ApplyStyleToElement(this XmlWriter writer, StyleResourceNames style)
        {
            writer.WriteAttributeString("Style", $"{{DynamicResource {style}}}");
        }

    }
}
