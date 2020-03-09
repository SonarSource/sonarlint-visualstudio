/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.LocalServices.TestProjectIndicators
{
    public class ServiceGuidTestProjectIndicator : ITestProjectIndicator
    {
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private const string TestServiceGuid = "{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}";

        public ServiceGuidTestProjectIndicator(ILogger logger)
            : this(logger, new FileSystem())
        {
        }

        internal ServiceGuidTestProjectIndicator(ILogger logger, IFileSystem fileSystem)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public bool? IsTestProject(Project project)
        {
            try
            {
                var hasTestGuid = HasTestGuid(project.FullName);

                return hasTestGuid ? true : (bool?) null;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.FailedToCheckIfTestProject, project.UniqueName, ex.ToString());

                return null;
            }
        }

        private bool HasTestGuid(string projectPath)
        {
            var projectXml = fileSystem.File.ReadAllText(projectPath);
            var xDocument = XDocument.Load(new StringReader(projectXml));
            var xPathEvaluate = xDocument.XPathEvaluate("//Project//ItemGroup//Service/@Include") as IEnumerable;

            var hasTestGuid = xPathEvaluate
                .Cast<XAttribute>()
                .Any(x => string.Equals(x.Value, TestServiceGuid, StringComparison.OrdinalIgnoreCase));

            return hasTestGuid;
        }
    }
}
