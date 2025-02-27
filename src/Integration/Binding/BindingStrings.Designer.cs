﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Binding {
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
    internal class BindingStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal BindingStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.Integration.Binding.BindingStrings", typeof(BindingStrings).Assembly);
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
        ///   Looks up a localized string similar to Failed to add SonarLint.xml file to the project &apos;{0}&apos;.
        ///Error: {1}.
        /// </summary>
        internal static string CSharpVB_FailedToAddSonarLintXml {
            get {
                return ResourceManager.GetString("CSharpVB_FailedToAddSonarLintXml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to set ItemType for SonarLint.xml in the project &apos;{0}&apos;.
        ///The project already references the SonarLint.xml file but with the wrong Item type.
        ///Please change the Item type to &apos;{1}&apos; and try again.
        ///Error: {2}.
        /// </summary>
        internal static string CSharpVB_FailedToSetSonarLintXmlItemType {
            get {
                return ResourceManager.GetString("CSharpVB_FailedToSetSonarLintXmlItemType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ProjectLanguageIndicator] Failed to identify languages files in project &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string FailedToIdentifyLanguage {
            get {
                return ResourceManager.GetString("FailedToIdentifyLanguage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to bind project &apos;{0}&apos;.
        ///A conflicting version of {1} has been found. Please delete the file &apos;{2}&apos; and remove references to it from your projects, then re-open the solution and try again..
        /// </summary>
        internal static string FoundConflictingAdditionalFile {
            get {
                return ResourceManager.GetString("FoundConflictingAdditionalFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configure Binding.
        /// </summary>
        internal static string NoBindingSuggestionNotification_ConfigureBindingAction {
            get {
                return ResourceManager.GetString("NoBindingSuggestionNotification_ConfigureBindingAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Learn more.
        /// </summary>
        internal static string NoBindingSuggestionNotification_LearnMoreAction {
            get {
                return ResourceManager.GetString("NoBindingSuggestionNotification_LearnMoreAction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ConnectedMode: SonarQube for Visual Studio couldn&apos;t match the {0} project &apos;{1}&apos; to the currently opened solution. Please make sure the solution is opened, or try configuring the binding manually..
        /// </summary>
        internal static string NoBindingSuggestionNotification_Message {
            get {
                return ResourceManager.GetString("NoBindingSuggestionNotification_Message", resourceCulture);
            }
        }
    }
}
