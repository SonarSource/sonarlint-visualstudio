﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Resources {
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
    public class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.Integration.Resources.Strings", typeof(Strings).Assembly);
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
        ///   Looks up a localized string similar to Cancel.
        /// </summary>
        public static string CancelButtonText {
            get {
                return ResourceManager.GetString("CancelButtonText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A SonarQube (Cloud, Server) server plugin has a malformed version and cannot be compared. Version was &apos;{0}&apos;..
        /// </summary>
        public static string CannotCompareVersionStrings {
            get {
                return ResourceManager.GetString("CannotCompareVersionStrings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connections.
        /// </summary>
        public static string ConnectSectionTitle {
            get {
                return ResourceManager.GetString("ConnectSectionTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No file was found under &apos;{0}&apos;.
        /// </summary>
        public static string ExclusionFileNotFound {
            get {
                return ResourceManager.GetString("ExclusionFileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error loading server settings. Analysis settings from the server (e.g. inclusions/exclusions) will not be applied in the IDE. Error: {0}.
        /// </summary>
        public static string ExclusionGetError {
            get {
                return ResourceManager.GetString("ExclusionGetError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test project regular expression pattern &apos;{0}&apos; is invalid. The default will be used instead. Please check your server settings..
        /// </summary>
        public static string InvalidTestProjectRegexPattern {
            get {
                return ResourceManager.GetString("InvalidTestProjectRegexPattern", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The issue is valid but will not be fixed now. It represents accepted technical debt..
        /// </summary>
        public static string MuteWindow_AcceptContent {
            get {
                return ResourceManager.GetString("MuteWindow_AcceptContent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Accept.
        /// </summary>
        public static string MuteWindow_AcceptTitle {
            get {
                return ResourceManager.GetString("MuteWindow_AcceptTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cancel.
        /// </summary>
        public static string MuteWindow_CancelButton {
            get {
                return ResourceManager.GetString("MuteWindow_CancelButton", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add a comment (optional):.
        /// </summary>
        public static string MuteWindow_CommentLabel {
            get {
                return ResourceManager.GetString("MuteWindow_CommentLabel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The issue is raised unxpectedly on code that should not trigger an issue..
        /// </summary>
        public static string MuteWindow_FalsePositiveContent {
            get {
                return ResourceManager.GetString("MuteWindow_FalsePositiveContent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to False Positive.
        /// </summary>
        public static string MuteWindow_FalsePositiveTitle {
            get {
                return ResourceManager.GetString("MuteWindow_FalsePositiveTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to : *Bold*, ``Code``, * Bullet point.
        /// </summary>
        public static string MuteWindow_FormattingHelpExamples {
            get {
                return ResourceManager.GetString("MuteWindow_FormattingHelpExamples", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Formatting Help.
        /// </summary>
        public static string MuteWindow_FormattingHelpLink {
            get {
                return ResourceManager.GetString("MuteWindow_FormattingHelpLink", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mark Issue as....
        /// </summary>
        public static string MuteWindow_SubmitButton {
            get {
                return ResourceManager.GetString("MuteWindow_SubmitButton", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mark Issue as Resolved on SonarQube (Cloud, Server).
        /// </summary>
        public static string MuteWindow_Title {
            get {
                return ResourceManager.GetString("MuteWindow_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The issue is valid but does not need fixing. It represents accepted technical debt..
        /// </summary>
        public static string MuteWindow_WontFixContent {
            get {
                return ResourceManager.GetString("MuteWindow_WontFixContent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Won&apos;t Fix.
        /// </summary>
        public static string MuteWindow_WontFixTitle {
            get {
                return ResourceManager.GetString("MuteWindow_WontFixTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to fetch notifications: {0}.
        /// </summary>
        public static string Notifications_ERROR_Fetching {
            get {
                return ResourceManager.GetString("Notifications_ERROR_Fetching", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Notifications are not supported on this version of SonarQube (Cloud, Server).
        /// </summary>
        public static string Notifications_NotSupported {
            get {
                return ResourceManager.GetString("Notifications_NotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube for Visual Studio.
        /// </summary>
        public static string SonarLintOutputPaneTitle {
            get {
                return ResourceManager.GetString("SonarLintOutputPaneTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube (Cloud, Server).
        /// </summary>
        public static string TeamExplorerPageTitle {
            get {
                return ResourceManager.GetString("TeamExplorerPageTitle", resourceCulture);
            }
        }
    }
}
