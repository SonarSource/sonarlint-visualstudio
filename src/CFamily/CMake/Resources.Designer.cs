﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.CFamily.CMake {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.CFamily.CMake.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to [CMake] Could not parse CMakeSettings.json: {0}.
        /// </summary>
        internal static string BadCMakeSettings {
            get {
                return ResourceManager.GetString("BadCMakeSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [CMake] Could not parse compile_commands.json: {0}.
        /// </summary>
        internal static string BadCompilationDatabaseFile {
            get {
                return ResourceManager.GetString("BadCompilationDatabaseFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [CMake] Specified build configuration &apos;{0}&apos; could not be found at &apos;{1}&apos;.
        /// </summary>
        internal static string NoBuildConfigInCMakeSettings {
            get {
                return ResourceManager.GetString("NoBuildConfigInCMakeSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [CMake] Could not find file in the compilation database. File: &apos;{0}&apos;.
        /// </summary>
        internal static string NoCompilationDatabaseEntry {
            get {
                return ResourceManager.GetString("NoCompilationDatabaseEntry", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [CMake] Could not locate compilation database at &apos;{0}&apos;. Make sure that your project is configured correctly. 
        ///    See https://github.com/SonarSource/sonarlint-visualstudio/wiki for more information..
        /// </summary>
        internal static string NoCompilationDatabaseFile {
            get {
                return ResourceManager.GetString("NoCompilationDatabaseFile", resourceCulture);
            }
        }
    }
}
