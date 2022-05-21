﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily {
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
    internal class CFamilyStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CFamilyStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.CFamily.Subprocess.CFamilyStrings", typeof(CFamilyStrings).Assembly);
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
        ///   Looks up a localized string similar to Execution failed. The specified executable does not exist: {0}.
        /// </summary>
        internal static string ERROR_ProcessRunner_ExeNotFound {
            get {
                return ResourceManager.GetString("ERROR_ProcessRunner_ExeNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;sensitive data removed&gt;.
        /// </summary>
        internal static string MSG_CmdLine_SensitiveCmdLineArgsAlternativeText {
            get {
                return ResourceManager.GetString("MSG_CmdLine_SensitiveCmdLineArgsAlternativeText", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Executing file {0}
        ///  Args: {1}
        ///  Working directory: {2}
        ///  Process id: {3}.
        /// </summary>
        internal static string MSG_ExecutingFile {
            get {
                return ResourceManager.GetString("MSG_ExecutingFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Execution cancelled..
        /// </summary>
        internal static string MSG_ExecutionCancelled {
            get {
                return ResourceManager.GetString("MSG_ExecutionCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process returned exit code {0}.
        /// </summary>
        internal static string MSG_ExecutionExitCode {
            get {
                return ResourceManager.GetString("MSG_ExecutionExitCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DEBUG: .
        /// </summary>
        internal static string MSG_Prefix_DEBUG {
            get {
                return ResourceManager.GetString("MSG_Prefix_DEBUG", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ERROR: .
        /// </summary>
        internal static string MSG_Prefix_ERROR {
            get {
                return ResourceManager.GetString("MSG_Prefix_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WARNING: .
        /// </summary>
        internal static string MSG_Prefix_WARN {
            get {
                return ResourceManager.GetString("MSG_Prefix_WARN", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process {0} was killed..
        /// </summary>
        internal static string MSG_ProessRunner_ProcessKilled {
            get {
                return ResourceManager.GetString("MSG_ProessRunner_ProcessKilled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Overwriting the value of environment variable &apos;{0}&apos;. Old value: {1}, new value: {2}.
        /// </summary>
        internal static string MSG_Runner_OverwritingEnvVar {
            get {
                return ResourceManager.GetString("MSG_Runner_OverwritingEnvVar", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Setting environment variable &apos;{0}&apos;. Value: {1}.
        /// </summary>
        internal static string MSG_Runner_SettingEnvVar {
            get {
                return ResourceManager.GetString("MSG_Runner_SettingEnvVar", resourceCulture);
            }
        }
    }
}
