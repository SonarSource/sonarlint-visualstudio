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
        ///   Looks up a localized string similar to Internal analysis failure. See logs above..
        /// </summary>
        public static string AnalysisFailedReason {
            get {
                return ResourceManager.GetString("AnalysisFailedReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updated analysis readiness: {0}.
        /// </summary>
        public static string AnalysisReadinessUpdate {
            get {
                return ResourceManager.GetString("AnalysisReadinessUpdate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [CertificateChainValidator] Certificate validation failed for the following reason(s):.
        /// </summary>
        public static string CertificateValidator_Failed {
            get {
                return ResourceManager.GetString("CertificateValidator_Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [CertificateChainValidator] {0}: {1}.
        /// </summary>
        public static string CertificateValidator_FailureReasonTemplate {
            get {
                return ResourceManager.GetString("CertificateValidator_FailureReasonTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [SLCoreAnalyzer] No compilation database found for file: {0}. Check that the file is part of a supported project type in the current solution..
        /// </summary>
        public static string CompilationDatabaseNotFound {
            get {
                return ResourceManager.GetString("CompilationDatabaseNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configuration scope conflict.
        /// </summary>
        public static string ConfigScopeConflict {
            get {
                return ResourceManager.GetString("ConfigScopeConflict", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Configuration scope not initialized.
        /// </summary>
        public static string ConfigScopeNotInitialized {
            get {
                return ResourceManager.GetString("ConfigScopeNotInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Received server trust verification request....
        /// </summary>
        public static string HttpConfiguration_ServerTrustVerificationRequest {
            get {
                return ResourceManager.GetString("HttpConfiguration_ServerTrustVerificationRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server verification result: {0}.
        /// </summary>
        public static string HttpConfiguration_ServerTrustVerificationResult {
            get {
                return ResourceManager.GetString("HttpConfiguration_ServerTrustVerificationResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected enum value.
        /// </summary>
        public static string ModelExtensions_UnexpectedValue {
            get {
                return ResourceManager.GetString("ModelExtensions_UnexpectedValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Creating analysis issue for {0} failed due to {1}..
        /// </summary>
        public static string RaiseFindingToAnalysisIssueConverter_CreateAnalysisIssueFailed {
            get {
                return ResourceManager.GetString("RaiseFindingToAnalysisIssueConverter_CreateAnalysisIssueFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Path must be fully-qualified. ({0}).
        /// </summary>
        public static string RelativePathHelper_NonAbsolutePath {
            get {
                return ResourceManager.GetString("RelativePathHelper_NonAbsolutePath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Root path must end with a {0} separator.
        /// </summary>
        public static string RelativePathHelper_RootDoesNotEndWithSeparator {
            get {
                return ResourceManager.GetString("RelativePathHelper_RootDoesNotEndWithSeparator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The server certificate can not be verified. Please see the logs for more info..
        /// </summary>
        public static string ServerCertificateInfobar_CertificateInvalidMessage {
            get {
                return ResourceManager.GetString("ServerCertificateInfobar_CertificateInvalidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Learn more.
        /// </summary>
        public static string ServerCertificateInfobar_LearnMore {
            get {
                return ResourceManager.GetString("ServerCertificateInfobar_LearnMore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Show logs.
        /// </summary>
        public static string ServerCertificateInfobar_ShowLogs {
            get {
                return ResourceManager.GetString("ServerCertificateInfobar_ShowLogs", resourceCulture);
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
        ///   Looks up a localized string similar to SLCore.
        /// </summary>
        public static string SLCoreName {
            get {
                return ResourceManager.GetString("SLCoreName", resourceCulture);
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
        ///   Looks up a localized string similar to SonarQube for Visual Studio background service failed to start.
        /// </summary>
        public static string SloopRestartFailedNotificationService_GoldBarMessage {
            get {
                return ResourceManager.GetString("SloopRestartFailedNotificationService_GoldBarMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Restart SonarQube for Visual Studio.
        /// </summary>
        public static string SloopRestartFailedNotificationService_Restart {
            get {
                return ResourceManager.GetString("SloopRestartFailedNotificationService_Restart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected server connection type.
        /// </summary>
        public static string UnexpectedServerConnectionType {
            get {
                return ResourceManager.GetString("UnexpectedServerConnectionType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Proxy type can not be determined from scheme &apos;{0}&apos;. Returning HTTP proxy type..
        /// </summary>
        public static string UnknowProxyType {
            get {
                return ResourceManager.GetString("UnknowProxyType", resourceCulture);
            }
        }
    }
}
