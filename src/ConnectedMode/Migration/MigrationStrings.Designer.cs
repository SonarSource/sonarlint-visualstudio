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
        ///   Looks up a localized string similar to [Migration] Cleaning files....
        /// </summary>
        internal static string CleaningFiles {
            get {
                return ResourceManager.GetString("CleaningFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Number of files to clean: {0}.
        /// </summary>
        internal static string CountOfFilesToClean {
            get {
                return ResourceManager.GetString("CountOfFilesToClean", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Deleting the legacy .sonarlint....
        /// </summary>
        internal static string DeletingSonarLintFolder {
            get {
                return ResourceManager.GetString("DeletingSonarLintFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Error during migration: {0}.
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
        ///   Looks up a localized string similar to [Migration] Finished ConnectedMode migration..
        /// </summary>
        internal static string Finished {
            get {
                return ResourceManager.GetString("Finished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Getting list of files to clean....
        /// </summary>
        internal static string GettingFiles {
            get {
                return ResourceManager.GetString("GettingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Creating new binding configuration....
        /// </summary>
        internal static string ProcessingNewBinding {
            get {
                return ResourceManager.GetString("ProcessingNewBinding", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Saving files....
        /// </summary>
        internal static string SavingFiles {
            get {
                return ResourceManager.GetString("SavingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Skipping cleaning files: no files were found that could contain settings that need to cleaned..
        /// </summary>
        internal static string SkippingCleaning {
            get {
                return ResourceManager.GetString("SkippingCleaning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Migration] Starting Connected Mode migration....
        /// </summary>
        internal static string Starting {
            get {
                return ResourceManager.GetString("Starting", resourceCulture);
            }
        }
    }
}
