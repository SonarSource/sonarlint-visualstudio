﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.IssueVisualization {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.IssueVisualization.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Code fix suggestion.
        /// </summary>
        internal static string DiffViewWindow_DefaultCaption {
            get {
                return ResourceManager.GetString("DiffViewWindow_DefaultCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Apply fix suggestion {0} out of {1} in file {2}.
        /// </summary>
        internal static string DiffViewWindow_Title {
            get {
                return ResourceManager.GetString("DiffViewWindow_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to get image moniker for file &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string ERR_FailedToGetFileImageMoniker {
            get {
                return ResourceManager.GetString("ERR_FailedToGetFileImageMoniker", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error handling buffer change: {0} .
        /// </summary>
        internal static string ERR_HandlingBufferChange {
            get {
                return ResourceManager.GetString("ERR_HandlingBufferChange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error navigating to analysis issue location: {0}.
        /// </summary>
        internal static string ERR_NavigationException {
            get {
                return ResourceManager.GetString("ERR_NavigationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error opening file &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string ERR_OpenDocumentException {
            get {
                return ResourceManager.GetString("ERR_OpenDocumentException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error querying status of Visualization Tool Window command: {0}.
        /// </summary>
        internal static string ERR_QueryStatusVisualizationToolWindowCommand {
            get {
                return ResourceManager.GetString("ERR_QueryStatusVisualizationToolWindowCommand", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Quick fixes] Error processing quick fixes: {0}.
        /// </summary>
        internal static string ERR_QuickFixes_Exception {
            get {
                return ResourceManager.GetString("ERR_QuickFixes_Exception", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error displaying Visualization Tool Window: {0}.
        /// </summary>
        internal static string ERR_VisualizationToolWindow_Exception {
            get {
                return ResourceManager.GetString("ERR_VisualizationToolWindow_Exception", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sonar Issue Visualization.
        /// </summary>
        internal static string IssueVisualizationToolWindowCaption {
            get {
                return ResourceManager.GetString("IssueVisualizationToolWindowCaption", resourceCulture);
            }
        }
    }
}
