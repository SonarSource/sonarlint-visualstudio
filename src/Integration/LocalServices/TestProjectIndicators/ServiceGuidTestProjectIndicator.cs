using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.LocalServices.TestProjectIndicators
{
    public class ServiceGuidTestProjectIndicator : ITestProjectIndicator
    {
        private readonly IFileSystem fileSystem;
        private const string TestServiceGuid = "{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}";

        public ServiceGuidTestProjectIndicator(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public bool? IsTestProject(Project project)
        {
            var projectPath = project.FullName;
            var projectXml = fileSystem.File.ReadAllText(projectPath);
            var xDocument = XDocument.Load(new StringReader(projectXml));
            var xPathEvaluate = xDocument.XPathEvaluate("//Project//ItemGroup//Service/@Include") as IEnumerable;
            var hasTestGuid = xPathEvaluate.Cast<XAttribute>().Any(x =>
                string.Equals(x.Value, TestServiceGuid, StringComparison.OrdinalIgnoreCase));

            return hasTestGuid ? true : (bool?) null;
        }
    }
}
