﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.SLCore {
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
    public class SLCoreStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SLCoreStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.SLCore.SLCoreStrings", typeof(SLCoreStrings).Assembly);
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
        ///   Looks up a localized string similar to Service Provider is unavailable.
        /// </summary>
        public static string ServiceProviderNotInitialized {
            get {
                return ResourceManager.GetString("ServiceProviderNotInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreHandler] Creating SLCore instance.
        /// </summary>
        public static string SLCoreHandler_CreatingInstance {
            get {
                return ResourceManager.GetString("SLCoreHandler_CreatingInstance", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreHandler] Error creating SLCore instance.
        /// </summary>
        public static string SLCoreHandler_CreatingInstanceError {
            get {
                return ResourceManager.GetString("SLCoreHandler_CreatingInstanceError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Current instance is alive.
        /// </summary>
        public static string SLCoreHandler_InstanceAlreadyRunning {
            get {
                return ResourceManager.GetString("SLCoreHandler_InstanceAlreadyRunning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreHandler] SLCore instance exited.
        /// </summary>
        public static string SLCoreHandler_InstanceDied {
            get {
                return ResourceManager.GetString("SLCoreHandler_InstanceDied", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreHandler] Starting SLCore instance.
        /// </summary>
        public static string SLCoreHandler_StartingInstance {
            get {
                return ResourceManager.GetString("SLCoreHandler_StartingInstance", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreHandler] Error starting SLCore instance.
        /// </summary>
        public static string SLCoreHandler_StartingInstanceError {
            get {
                return ResourceManager.GetString("SLCoreHandler_StartingInstanceError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreServiceProvider]Cannot Create Service. Error: {0}.
        /// </summary>
        public static string SLCoreServiceProvider_CreateServiceError {
            get {
                return ResourceManager.GetString("SLCoreServiceProvider_CreateServiceError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarLint background service failed to start.
        /// </summary>
        public static string SloopRestartFailedNotificationService_GoldBarMessage {
            get {
                return ResourceManager.GetString("SloopRestartFailedNotificationService_GoldBarMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restart SonarLint.
        /// </summary>
        public static string SloopRestartFailedNotificationService_Restart {
            get {
                return ResourceManager.GetString("SloopRestartFailedNotificationService_Restart", resourceCulture);
            }
        }
    }
}
