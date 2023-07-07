﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.ConnectedMode.Migration {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class MigrationStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal MigrationStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.ConnectedMode.Migration.MigrationStrings", typeof(MigrationStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Migration cancelled by user.
        /// </summary>
        internal static string CancelTokenFailure_NormalLog {
            get {
                return ResourceManager.GetString("CancelTokenFailure_NormalLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [MigrationWizardXaml] Migration cancelled by user: {0}.
        /// </summary>
        internal static string CancelTokenFailure_VerboseLog {
            get {
                return ResourceManager.GetString("CancelTokenFailure_VerboseLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Removing {0} settings from file....
        /// </summary>
        internal static string Cleaner_RemovingSettings {
            get {
                return ResourceManager.GetString("Cleaner_RemovingSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Error during migration: {0}
        ///  Run migration again with verbose logging enabled for more information..
        /// </summary>
        internal static string ErrorDuringMigation_NormalLog {
            get {
                return ResourceManager.GetString("ErrorDuringMigation_NormalLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [MigrationWizardXaml] Error during migration: {0}.
        /// </summary>
        internal static string ErrorDuringMigation_VerboseLog {
            get {
                return ResourceManager.GetString("ErrorDuringMigation_VerboseLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarLint: You have both old and new connected mode settings. Please Migrate again to clean up the old settings.
        /// </summary>
        internal static string MigrationPrompt_AlreadyConnected_Message {
            get {
                return ResourceManager.GetString("MigrationPrompt_AlreadyConnected_Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Learn more.
        /// </summary>
        internal static string MigrationPrompt_LearnMoreButton {
            get {
                return ResourceManager.GetString("MigrationPrompt_LearnMoreButton", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarLint: You&apos;re using an outdated Connected Mode configuration format. Some features will not be available. Please migrate..
        /// </summary>
        internal static string MigrationPrompt_Message {
            get {
                return ResourceManager.GetString("MigrationPrompt_Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migrate configuration.
        /// </summary>
        internal static string MigrationPrompt_MigrateButton {
            get {
                return ResourceManager.GetString("MigrationPrompt_MigrateButton", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration]   - file will be updated.
        /// </summary>
        internal static string Process_CheckedFile_Changed {
            get {
                return ResourceManager.GetString("Process_CheckedFile_Changed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration]   - no changes.
        /// </summary>
        internal static string Process_CheckedFile_Unchanged {
            get {
                return ResourceManager.GetString("Process_CheckedFile_Unchanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Checking file {0} of {1}: {2}.
        /// </summary>
        internal static string Process_CheckingFile {
            get {
                return ResourceManager.GetString("Process_CheckingFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Checking files for Connected Mode settings....
        /// </summary>
        internal static string Process_CheckingFiles {
            get {
                return ResourceManager.GetString("Process_CheckingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Number of files to check: {0}.
        /// </summary>
        internal static string Process_CountOfFilesToCheck {
            get {
                return ResourceManager.GetString("Process_CountOfFilesToCheck", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Deleting the legacy .sonarlint....
        /// </summary>
        internal static string Process_DeletingSonarLintFolder {
            get {
                return ResourceManager.GetString("Process_DeletingSonarLintFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Finished ConnectedMode migration..
        /// </summary>
        internal static string Process_Finished {
            get {
                return ResourceManager.GetString("Process_Finished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Getting list of files to check....
        /// </summary>
        internal static string Process_GettingFiles {
            get {
                return ResourceManager.GetString("Process_GettingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration]  - {0}.
        /// </summary>
        internal static string Process_ListChangedFile {
            get {
                return ResourceManager.GetString("Process_ListChangedFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Number of files to modify: {0}.
        /// </summary>
        internal static string Process_NumberOfChangedFiles {
            get {
                return ResourceManager.GetString("Process_NumberOfChangedFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Creating new binding configuration....
        /// </summary>
        internal static string Process_ProcessingNewBinding {
            get {
                return ResourceManager.GetString("Process_ProcessingNewBinding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Saving files....
        /// </summary>
        internal static string Process_SavingFiles {
            get {
                return ResourceManager.GetString("Process_SavingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Skipping checking files: no files were found that could contain Connected Mode settings..
        /// </summary>
        internal static string Process_SkippingChecking {
            get {
                return ResourceManager.GetString("Process_SkippingChecking", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Starting Connected Mode migration....
        /// </summary>
        internal static string Process_Starting {
            get {
                return ResourceManager.GetString("Process_Starting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://github.com/SonarSource/sonarlint-visualstudio/wiki/migrate-connected-mode-to-v7.
        /// </summary>
        internal static string Url_LearnMoreUrl {
            get {
                return ResourceManager.GetString("Url_LearnMoreUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://github.com/SonarSource/sonarlint-visualstudio/wiki/migrate-connected-mode-to-v7-tfvc.
        /// </summary>
        internal static string Url_TfvcHelp {
            get {
                return ResourceManager.GetString("Url_TfvcHelp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to check out file for editing: {0}.
        /// </summary>
        internal static string VSFileSystem_Error_FailedToCheckOutFile {
            get {
                return ResourceManager.GetString("VSFileSystem_Error_FailedToCheckOutFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to check out folder contents for deletion: {0}.
        /// </summary>
        internal static string VsFileSystem_Error_FailedToCheckOutFolderForDeletion {
            get {
                return ResourceManager.GetString("VsFileSystem_Error_FailedToCheckOutFolderForDeletion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to check out files for editing. Error flags: {0}.
        /// </summary>
        internal static string VSFileSystem_FailedToCheckOutFilesForEditing {
            get {
                return ResourceManager.GetString("VSFileSystem_FailedToCheckOutFilesForEditing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to check out files for saving. Error code: {0}.
        /// </summary>
        internal static string VSFileSystem_FailedToCheckOutFilesForSave {
            get {
                return ResourceManager.GetString("VSFileSystem_FailedToCheckOutFilesForSave", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migration cancelled.
        /// </summary>
        internal static string Wizard_Progress_Cancelled {
            get {
                return ResourceManager.GetString("Wizard_Progress_Cancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error during migration. See the Output Window for more information..
        /// </summary>
        internal static string Wizard_Progress_Error {
            get {
                return ResourceManager.GetString("Wizard_Progress_Error", resourceCulture);
            }
        }
    }
}
