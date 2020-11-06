using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Integration.Helpers
{
    [Export(typeof(IAbsoluteFilePathLocator))]
    internal class AbsoluteFilePathLocator : IAbsoluteFilePathLocator
    {
        private readonly IProjectSystemHelper projectSystemHelper;

        [ImportingConstructor]
        public AbsoluteFilePathLocator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : this(new ProjectSystemHelper(serviceProvider))
        {
        }

        internal AbsoluteFilePathLocator(IProjectSystemHelper projectSystemHelper)
        {
            this.projectSystemHelper = projectSystemHelper;
        }

        public string Locate(string relativeFilePath)
        {
            if (relativeFilePath == null)
            {
                throw new ArgumentNullException(nameof(relativeFilePath));
            }

            foreach (var vsHierarchy in projectSystemHelper.EnumerateProjects())
            {
                var vsProject = vsHierarchy as IVsProject;
                var projectFilePath = projectSystemHelper.GetItemFilePath(vsProject, VSConstants.VSITEMID.Root);

                if (string.IsNullOrEmpty(projectFilePath))
                {
                    continue;
                }

                var itemIdsInProject = projectSystemHelper.GetAllItems(vsHierarchy);

                foreach (var vsItemId in itemIdsInProject)
                {
                    var absoluteItemFilePath = projectSystemHelper.GetItemFilePath(vsProject, vsItemId);

                    if (string.IsNullOrEmpty(absoluteItemFilePath))
                    {
                        continue;
                    }

                    if (absoluteItemFilePath.EndsWith(relativeFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return absoluteItemFilePath;
                    }
                }
            }

            return null;
        }
    }
}
