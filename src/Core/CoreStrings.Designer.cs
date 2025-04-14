﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
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
        ///   Looks up a localized string similar to Unexpected type {0} (expected {1}) for {2}.
        /// </summary>
        public static string CommaSeparatedStringArrayConverter_UnexpectedType {
            get {
                return ResourceManager.GetString("CommaSeparatedStringArrayConverter_UnexpectedType", resourceCulture);
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
        ///   Looks up a localized string similar to Failed to {0} credentials, Win32ErrorCode: {1}. See https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes for more information..
        /// </summary>
        public static string CredentialStore_Win32Error {
            get {
                return ResourceManager.GetString("CredentialStore_Win32Error", resourceCulture);
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
        ///   Looks up a localized string similar to hotspot.
        /// </summary>
        public static string FindingType_Hotspot {
            get {
                return ResourceManager.GetString("FindingType_Hotspot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to issue.
        /// </summary>
        public static string FindingType_Issue {
            get {
                return ResourceManager.GetString("FindingType_Issue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Solution/folder is not in a git repository.
        /// </summary>
        public static string NoGitFolder {
            get {
                return ResourceManager.GetString("NoGitFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Don&apos;t show this again.
        /// </summary>
        public static string Notifications_DontShowAgainAction {
            get {
                return ResourceManager.GetString("Notifications_DontShowAgainAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Notifications] Failed to display notification &quot;{0}&quot;: {1}.
        /// </summary>
        public static string Notifications_FailedToDisplay {
            get {
                return ResourceManager.GetString("Notifications_FailedToDisplay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Notifications] Failed to execute notification action: {0}.
        /// </summary>
        public static string Notifications_FailedToExecuteAction {
            get {
                return ResourceManager.GetString("Notifications_FailedToExecuteAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Notifications] Failed to remove existing notification: {0}.
        /// </summary>
        public static string Notifications_FailedToRemove {
            get {
                return ResourceManager.GetString("Notifications_FailedToRemove", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An absolute file path was expected..
        /// </summary>
        public static string PathHelperAbsolutePathExpected {
            get {
                return ResourceManager.GetString("PathHelperAbsolutePathExpected", resourceCulture);
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
        ///   Looks up a localized string similar to SonarQube Cloud.
        /// </summary>
        public static string SonarQubeCloudProductName {
            get {
                return ResourceManager.GetString("SonarQubeCloudProductName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube (Server, Cloud) request failed: {0} {1}.
        /// </summary>
        public static string SonarQubeRequestFailed {
            get {
                return ResourceManager.GetString("SonarQubeRequestFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube (Server, Cloud) request timed out or was canceled.
        /// </summary>
        public static string SonarQubeRequestTimeoutOrCancelled {
            get {
                return ResourceManager.GetString("SonarQubeRequestTimeoutOrCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube Server.
        /// </summary>
        public static string SonarQubeServerProductName {
            get {
                return ResourceManager.GetString("SonarQubeServerProductName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [RuleSettings] Unable to load/locate connected mode settings. Falling back on standalone mode settings..
        /// </summary>
        public static string UnableToLoadConnectedModeSettings {
            get {
                return ResourceManager.GetString("UnableToLoadConnectedModeSettings", resourceCulture);
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
        ///   Looks up a localized string similar to [RuleSettings] Using connected mode settings. User-specified settings in settings.json will be ignored..
        /// </summary>
        public static string UsingConnectedModeSettings {
            get {
                return ResourceManager.GetString("UsingConnectedModeSettings", resourceCulture);
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
