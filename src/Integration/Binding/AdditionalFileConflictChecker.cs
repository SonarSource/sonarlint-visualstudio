

using System;
using System.IO;
using System.IO.Abstractions;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal class AdditionalFileConflictChecker : IAdditionalFileConflictChecker
    {
        private readonly IFileSystem fileSystem;

        public AdditionalFileConflictChecker()
            : this(new FileSystem())
        {
        }

        internal AdditionalFileConflictChecker(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public bool HasConflictingAdditionalFile(Project project, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            return ExistsUnderRootFolder(project, additionalFileName, out conflictingAdditionalFilePath) ||
                   ExistsInProject(project, additionalFileName, out conflictingAdditionalFilePath);
        }

        private bool ExistsUnderRootFolder(Project project, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            var projectRootDirectory = Path.GetDirectoryName(project.FullName);
            conflictingAdditionalFilePath = Path.Combine(projectRootDirectory, additionalFileName);

            // For old-style MSBuild projects, the file can exist on disk but not referenced in the project, so we check using the file system
            return fileSystem.File.Exists(conflictingAdditionalFilePath);
        }

        private bool ExistsInProject(Project project, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                if (HasAdditionalFile(projectItem, additionalFileName, out conflictingAdditionalFilePath))
                {
                    return true;
                }
            }

            conflictingAdditionalFilePath = string.Empty;
            return false;
        }

        private bool HasAdditionalFile(ProjectItem projectItem, string additionalFileName, out string conflictingAdditionalFilePath)
        {
            if (additionalFileName.Equals(Path.GetFileName(projectItem.FileNames[0]), StringComparison.OrdinalIgnoreCase))
            {
                var itemTypeProperty = VsShellUtils.FindProperty(projectItem.Properties, Constants.ItemTypePropertyKey);
                var isMarkedAsAdditionalFile = Constants.AdditionalFilesItemTypeName.Equals(itemTypeProperty.Value?.ToString(), StringComparison.OrdinalIgnoreCase);

                if (isMarkedAsAdditionalFile)
                {
                    var pathProperty = VsShellUtils.FindProperty(projectItem.Properties, Constants.FullPathPropertyKey);
                    conflictingAdditionalFilePath = pathProperty.Value.ToString();
                    return true;
                }
            }

            foreach (ProjectItem subItem in projectItem.ProjectItems)
            {
                if (HasAdditionalFile(subItem, additionalFileName, out conflictingAdditionalFilePath))
                {
                    return true;
                }
            }

            conflictingAdditionalFilePath = string.Empty;
            return false;
        }
    }
}
