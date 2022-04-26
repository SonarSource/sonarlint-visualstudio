﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Invalid response from eslint-bridge: {0}.
        /// </summary>
        internal static string ERR_InvalidResponse {
            get {
                return ResourceManager.GetString("ERR_InvalidResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [eslint-bridge] Failed to execute request to &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string ERR_RequestFailure {
            get {
                return ResourceManager.GetString("ERR_RequestFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [eslint-bridge] Started eslint-bridge process. Process id: {0}.
        /// </summary>
        internal static string INFO_ServerProcessId {
            get {
                return ResourceManager.GetString("INFO_ServerProcessId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [eslint-bridge] [process id: {0}] Server running on port: {1}..
        /// </summary>
        internal static string INFO_ServerStarted {
            get {
                return ResourceManager.GetString("INFO_ServerStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [eslint-bridge] [process id: {0}] Terminated eslint-bridge process..
        /// </summary>
        internal static string INFO_ServerTerminated {
            get {
                return ResourceManager.GetString("INFO_ServerTerminated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [eslint-bridge] [process id: {0}] Terminating eslint-bridge process....
        /// </summary>
        internal static string INFO_TerminatingServer {
            get {
                return ResourceManager.GetString("INFO_TerminatingServer", resourceCulture);
            }
        }
    }
}
