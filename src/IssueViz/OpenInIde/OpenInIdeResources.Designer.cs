﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.IssueVisualization.OpenInIde {
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
    internal class OpenInIdeResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal OpenInIdeResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.IssueVisualization.OpenInIde.OpenInIdeResources", typeof(OpenInIdeResources).Assembly);
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
        ///   Looks up a localized string similar to [Open in IDE] Unable to convert issue data: {0}.
        /// </summary>
        internal static string Converter_UnableToConvertIssueData {
            get {
                return ResourceManager.GetString("Converter_UnableToConvertIssueData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not handle Open in IDE request. See the Output Window for more information..
        /// </summary>
        internal static string DefaultInfoBarMessage {
            get {
                return ResourceManager.GetString("DefaultInfoBarMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Open in IDE] Could not find the location at File: {0}, Start Line: {1}, Start Position: {2}.
        /// </summary>
        internal static string IssueLocationNotFound {
            get {
                return ResourceManager.GetString("IssueLocationNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarLint Open in IDE.
        /// </summary>
        internal static string MessageBox_Caption {
            get {
                return ResourceManager.GetString("MessageBox_Caption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to process Open in IDE request. Reason: {0}.
        /// </summary>
        internal static string MessageBox_InvalidConfiguration {
            get {
                return ResourceManager.GetString("MessageBox_InvalidConfiguration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to process Open in IDE request. Reason: Invalid request.
        /// </summary>
        internal static string MessageBox_UnableToConvertIssue {
            get {
                return ResourceManager.GetString("MessageBox_UnableToConvertIssue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not locate issue. Ensure the file ({0}) has not been modified.
        /// </summary>
        internal static string MessageBox_UnableToLocateIssue {
            get {
                return ResourceManager.GetString("MessageBox_UnableToLocateIssue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not open File: {0}.
        /// </summary>
        internal static string MessageBox_UnableToOpenFile {
            get {
                return ResourceManager.GetString("MessageBox_UnableToOpenFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Open in IDE] Processing request. Configuration scope: {0}, Key: {1}, Type: {2}.
        /// </summary>
        internal static string ProcessingRequest {
            get {
                return ResourceManager.GetString("ProcessingRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Open in IDE] Configuration scope mismatch: Active scope {0} is different from received {1}.
        /// </summary>
        internal static string Validation_ConfigurationScopeMismatch {
            get {
                return ResourceManager.GetString("Validation_ConfigurationScopeMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Open in IDE] Configuration scope is not bound.
        /// </summary>
        internal static string Validation_ConfigurationScopeNotBound {
            get {
                return ResourceManager.GetString("Validation_ConfigurationScopeNotBound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Open in IDE] Configuration scope root path is not available.
        /// </summary>
        internal static string Validation_ConfigurationScopeRootNotSet {
            get {
                return ResourceManager.GetString("Validation_ConfigurationScopeRootNotSet", resourceCulture);
            }
        }
    }
}
