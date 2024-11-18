﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Education {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.Education.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Clean Code attributes are characteristics code needs to have to be considered clean.
        /// </summary>
        internal static string CCATooltip {
            get {
                return ResourceManager.GetString("CCATooltip", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Learn more about Clean Code.
        /// </summary>
        internal static string CleanCodeHyperLink {
            get {
                return ResourceManager.GetString("CleanCodeHyperLink", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Education] Unable to locate help for rule &apos;{0}&apos;. Help should be available at https://rules.sonarsource.com..
        /// </summary>
        internal static string Education_NoRuleInfo {
            get {
                return ResourceManager.GetString("Education_NoRuleInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Education] Error displaying Rule Help Window: {0}.
        /// </summary>
        internal static string ERR_RuleHelpToolWindow_Exception {
            get {
                return ResourceManager.GetString("ERR_RuleHelpToolWindow_Exception", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Education] Error handling Uri: {0}.
        /// </summary>
        internal static string ERR_RuleHelpUserControl_Exception {
            get {
                return ResourceManager.GetString("ERR_RuleHelpUserControl_Exception", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ErrorList] Event processor: Sonar rule detected: {0}.
        /// </summary>
        internal static string ErrorList_Processor_SonarRuleDetected {
            get {
                return ResourceManager.GetString("ErrorList_Processor_SonarRuleDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ErrorList] Event processor created.
        /// </summary>
        internal static string ErrorList_ProcessorCreated {
            get {
                return ResourceManager.GetString("ErrorList_ProcessorCreated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarQube Rule Help.
        /// </summary>
        internal static string RuleHelpToolWindowCaption {
            get {
                return ResourceManager.GetString("RuleHelpToolWindowCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Education] Failed to open Uri: {0}.
        /// </summary>
        internal static string RuleHelpUserControl_RelativeURI {
            get {
                return ResourceManager.GetString("RuleHelpUserControl_RelativeURI", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [RuleHelpUserControl] Failed to navigate to Uri as it is a relative path: {0}.
        /// </summary>
        internal static string RuleHelpUserControl_Verbose_RelativeURI {
            get {
                return ResourceManager.GetString("RuleHelpUserControl_Verbose_RelativeURI", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Issues found for this rule will have a {0} impact on the {1} of your software..
        /// </summary>
        internal static string SQTooltip {
            get {
                return ResourceManager.GetString("SQTooltip", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [Education] Error: rule description contains an unexpected node. Node type: {0}, name: {1}, value: {2}.
        /// </summary>
        internal static string XamlBuilder_UnexpectedNodeError {
            get {
                return ResourceManager.GetString("XamlBuilder_UnexpectedNodeError", resourceCulture);
            }
        }
    }
}
