﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Core {
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
    public class CoreStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CoreStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.Core.CoreStrings", typeof(CoreStrings).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to C++.
        /// </summary>
        public static string CppLanguageName {
            get {
                return ResourceManager.GetString("CppLanguageName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to C#.
        /// </summary>
        public static string CSharpLanguageName {
            get {
                return ResourceManager.GetString("CSharpLanguageName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Using cached rules settings.
        /// </summary>
        public static string EffectiveRules_CacheHit {
            get {
                return ResourceManager.GetString("EffectiveRules_CacheHit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Calculating effective rule settings....
        /// </summary>
        public static string EffectiveRules_CacheMiss {
            get {
                return ResourceManager.GetString("EffectiveRules_CacheMiss", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No user-specified rule settings - using defaults.
        /// </summary>
        public static string EffectiveRules_NoUserSettings {
            get {
                return ResourceManager.GetString("EffectiveRules_NoUserSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error loading settings from &quot;{0}&quot;.
        ///  Error message: {1}.
        /// </summary>
        public static string Settings_ErrorLoadingSettingsFile {
            get {
                return ResourceManager.GetString("Settings_ErrorLoadingSettingsFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error saving settings to &quot;{0}&quot;.
        ///  Error: {1}.
        /// </summary>
        public static string Settings_ErrorSavingSettingsFile {
            get {
                return ResourceManager.GetString("Settings_ErrorSavingSettingsFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loaded settings from &quot;{0}&quot;..
        /// </summary>
        public static string Settings_LoadedSettingsFile {
            get {
                return ResourceManager.GetString("Settings_LoadedSettingsFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Settings file does not exist at &quot;{0}&quot;..
        /// </summary>
        public static string Settings_NoSettingsFile {
            get {
                return ResourceManager.GetString("Settings_NoSettingsFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Saved settings to &quot;{0}&quot;..
        /// </summary>
        public static string Settings_SavedSettingsFile {
            get {
                return ResourceManager.GetString("Settings_SavedSettingsFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube request failed: {0} {1}.
        /// </summary>
        public static string SonarQubeRequestFailed {
            get {
                return ResourceManager.GetString("SonarQubeRequestFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube request timed out or was canceled.
        /// </summary>
        public static string SonarQubeRequestTimeoutOrCancelled {
            get {
                return ResourceManager.GetString("SonarQubeRequestTimeoutOrCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown.
        /// </summary>
        public static string UnknownLanguageName {
            get {
                return ResourceManager.GetString("UnknownLanguageName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to VB.NET.
        /// </summary>
        public static string VBNetLanguageName {
            get {
                return ResourceManager.GetString("VBNetLanguageName", resourceCulture);
            }
        }
    }
}
