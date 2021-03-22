﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.TypeScript.NodeJSLocator.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to [NodeJsLocator] File &apos;{0}&apos; does not exist..
        /// </summary>
        internal static string ERR_FileNotFound {
            get {
                return ResourceManager.GetString("ERR_FileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [NodeJsLocator] Found incompatible Node version: {0}. Path: {1}.
        /// </summary>
        internal static string ERR_IncompatibleVersion {
            get {
                return ResourceManager.GetString("ERR_IncompatibleVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [NodeJsLocator] Could not find a compatible node version. Supported versions are v10 and v12+..
        /// </summary>
        internal static string ERR_NoCompatibleVersion {
            get {
                return ResourceManager.GetString("ERR_NoCompatibleVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [NodeJsLocator] Environment variable value: {0}.
        /// </summary>
        internal static string INFO_EnvVarValue {
            get {
                return ResourceManager.GetString("INFO_EnvVarValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [NodeJsLocator] Found compatible Node version: {0}. Path: {1}.
        /// </summary>
        internal static string INFO_FoundCompatibleVersion {
            get {
                return ResourceManager.GetString("INFO_FoundCompatibleVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [NodeJsLocator] Environment variable is not set.
        /// </summary>
        internal static string INFO_NoEnvVar {
            get {
                return ResourceManager.GetString("INFO_NoEnvVar", resourceCulture);
            }
        }
    }
}
