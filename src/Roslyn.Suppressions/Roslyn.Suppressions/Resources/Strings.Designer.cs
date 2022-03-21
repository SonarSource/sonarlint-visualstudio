﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Roslyn.Suppressions.Resources {
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
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.Roslyn.Suppressions.Resources.Strings", typeof(Strings).Assembly);
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
        ///   Looks up a localized string similar to [Roslyn Suppressions] Error handling SonarLint suppressions change. Issues suppressed on the server may not be suppressed in the IDE. Error: {0}.
        /// </summary>
        internal static string FileWatcherException {
            get {
                return ResourceManager.GetString("FileWatcherException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Settings File was not found.
        /// </summary>
        internal static string RoslynSettingsFileStorageFileNotFound {
            get {
                return ResourceManager.GetString("RoslynSettingsFileStorageFileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Roslyn Suppressions] Error loading settings for project {0}. Issues suppressed on the server will not be suppressed in the IDE. Error: {1}.
        /// </summary>
        internal static string RoslynSettingsFileStorageGetError {
            get {
                return ResourceManager.GetString("RoslynSettingsFileStorageGetError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Roslyn Suppressions] Error writing settings for project {0}. Issues suppressed on the server may not be suppressed in the IDE. Error: {1}.
        /// </summary>
        internal static string RoslynSettingsFileStorageUpdateError {
            get {
                return ResourceManager.GetString("RoslynSettingsFileStorageUpdateError", resourceCulture);
            }
        }
    }
}
