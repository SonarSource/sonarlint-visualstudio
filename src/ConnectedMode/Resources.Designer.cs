﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.ConnectedMode {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.ConnectedMode.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Finished calculating closest Sonar server branch. Result: {0}.
        /// </summary>
        internal static string BranchMapper_CalculatingServerBranch_Finished {
            get {
                return ResourceManager.GetString("BranchMapper_CalculatingServerBranch_Finished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Calculating closest Sonar server branch....
        /// </summary>
        internal static string BranchMapper_CalculatingServerBranch_Started {
            get {
                return ResourceManager.GetString("BranchMapper_CalculatingServerBranch_Started", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping]     Checking Sonar server branches....
        /// </summary>
        internal static string BranchMapper_CheckingSonarBranches {
            get {
                return ResourceManager.GetString("BranchMapper_CheckingSonarBranches", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping]     Match found: same Sonar branch name: {0}.
        /// </summary>
        internal static string BranchMapper_Match_SameSonarBranchName {
            get {
                return ResourceManager.GetString("BranchMapper_Match_SameSonarBranchName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping]     No head branch detected .
        /// </summary>
        internal static string BranchMapper_NoHead {
            get {
                return ResourceManager.GetString("BranchMapper_NoHead", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping]     No match found - using Sonar server &quot;main&quot; branch.
        /// </summary>
        internal static string BranchMapper_NoMatchingBranchFound {
            get {
                return ResourceManager.GetString("BranchMapper_NoMatchingBranchFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping]     Updating closest match: branch = {0}, distance = {1}.
        /// </summary>
        internal static string BranchMapper_UpdatingClosestMatch {
            get {
                return ResourceManager.GetString("BranchMapper_UpdatingClosestMatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Could not detect a git repo for the current solution.
        /// </summary>
        internal static string BranchProvider_CouldNotDetectGitRepo {
            get {
                return ResourceManager.GetString("BranchProvider_CouldNotDetectGitRepo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Matching Sonar server branch: {0}.
        /// </summary>
        internal static string BranchProvider_MatchingServerBranchName {
            get {
                return ResourceManager.GetString("BranchProvider_MatchingServerBranchName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Solution is not bound.
        /// </summary>
        internal static string BranchProvider_NotInConnectedMode {
            get {
                return ResourceManager.GetString("BranchProvider_NotInConnectedMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {null}.
        /// </summary>
        internal static string NullBranchName {
            get {
                return ResourceManager.GetString("NullBranchName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Binding changed -&gt; cache cleared.
        /// </summary>
        internal static string StatefulBranchProvider_BindingChanged {
            get {
                return ResourceManager.GetString("StatefulBranchProvider_BindingChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Binding updated -&gt; cache cleared.
        /// </summary>
        internal static string StatefulBranchProvider_BindingUpdated {
            get {
                return ResourceManager.GetString("StatefulBranchProvider_BindingUpdated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Cache hit.
        /// </summary>
        internal static string StatefulBranchProvider_CacheHit {
            get {
                return ResourceManager.GetString("StatefulBranchProvider_CacheHit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Cache miss. Recalculating server branch mapping....
        /// </summary>
        internal static string StatefulBranchProvider_CacheMiss {
            get {
                return ResourceManager.GetString("StatefulBranchProvider_CacheMiss", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [ConnectedMode/BranchMapping] Closest Sonar server branch: {0}.
        /// </summary>
        internal static string StatefulBranchProvider_ReturnValue {
            get {
                return ResourceManager.GetString("StatefulBranchProvider_ReturnValue", resourceCulture);
            }
        }
    }
}
