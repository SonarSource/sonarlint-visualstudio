﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration.Vsix.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SonarLint.VisualStudio.Integration.Vsix.Resources.Strings", typeof(Strings).Assembly);
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
        ///   Looks up a localized string similar to Connected mode detected..
        /// </summary>
        internal static string AnalyzerManager_InConnectedMode {
            get {
                return ResourceManager.GetString("AnalyzerManager_InConnectedMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Standalone mode detected..
        /// </summary>
        internal static string AnalyzerManager_InStandaloneMode {
            get {
                return ResourceManager.GetString("AnalyzerManager_InStandaloneMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot find tool window with the specified guid: {0}.
        /// </summary>
        internal static string CannotFindToolWindow {
            get {
                return ResourceManager.GetString("CannotFindToolWindow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarLint - download error.
        /// </summary>
        internal static string Daemon_Download_ErrorDlgTitle {
            get {
                return ResourceManager.GetString("Daemon_Download_ErrorDlgTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Download error:.
        /// </summary>
        internal static string Daemon_Download_ErrorLogMessage {
            get {
                return ResourceManager.GetString("Daemon_Download_ErrorLogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Daemon download url: {0}.
        /// </summary>
        internal static string Daemon_Download_Url {
            get {
                return ResourceManager.GetString("Daemon_Download_Url", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished downloading the daemon..
        /// </summary>
        internal static string Daemon_Downloaded {
            get {
                return ResourceManager.GetString("Daemon_Downloaded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Downloading the daemon....
        /// </summary>
        internal static string Daemon_Downloading {
            get {
                return ResourceManager.GetString("Daemon_Downloading", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Downloading SonarLint daemon.
        /// </summary>
        internal static string Daemon_Downloading_StatusBarMessage {
            get {
                return ResourceManager.GetString("Daemon_Downloading_StatusBarMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Daemon error occurred in editor integration: {0}.
        /// </summary>
        internal static string Daemon_Editor_ERROR {
            get {
                return ResourceManager.GetString("Daemon_Editor_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished initializing the daemon package..
        /// </summary>
        internal static string Daemon_InitializationComplete {
            get {
                return ResourceManager.GetString("Daemon_InitializationComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initializing the daemon package....
        /// </summary>
        internal static string Daemon_Initializing {
            get {
                return ResourceManager.GetString("Daemon_Initializing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished installing the daemon..
        /// </summary>
        internal static string Daemon_Installed {
            get {
                return ResourceManager.GetString("Daemon_Installed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installing the daemon....
        /// </summary>
        internal static string Daemon_Installing {
            get {
                return ResourceManager.GetString("Daemon_Installing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The daemon file name supplied in {0} environment variable is invalid (&apos;{1}&apos;). Using the default download URL..
        /// </summary>
        internal static string Daemon_InvalidFileNameInDownloadEnvVar {
            get {
                return ResourceManager.GetString("Daemon_InvalidFileNameInDownloadEnvVar", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Download URL supplied in {0} environment variable is invalid (&apos;{1}&apos;). Using default download URL..
        /// </summary>
        internal static string Daemon_InvalidUrlInDownloadEnvVar {
            get {
                return ResourceManager.GetString("Daemon_InvalidUrlInDownloadEnvVar", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Not starting the daemon: support for additional languages is not activated..
        /// </summary>
        internal static string Daemon_NotStarting_NotEnabled {
            get {
                return ResourceManager.GetString("Daemon_NotStarting_NotEnabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file cannot be analyzed because the platform toolset has not been specified. Set the Platform Toolset property through the Visual Studio project property page..
        /// </summary>
        internal static string Daemon_PlatformToolsetNotSpecified {
            get {
                return ResourceManager.GetString("Daemon_PlatformToolsetNotSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Daemon started successfully..
        /// </summary>
        internal static string Daemon_Started {
            get {
                return ResourceManager.GetString("Daemon_Started", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting the daemon....
        /// </summary>
        internal static string Daemon_Starting {
            get {
                return ResourceManager.GetString("Daemon_Starting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Daemon stopped successfully..
        /// </summary>
        internal static string Daemon_Stopped {
            get {
                return ResourceManager.GetString("Daemon_Stopped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping the daemon....
        /// </summary>
        internal static string Daemon_Stopping {
            get {
                return ResourceManager.GetString("Daemon_Stopping", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Using default daemon download URL.
        /// </summary>
        internal static string Daemon_UsingDefaultDownloadLocation {
            get {
                return ResourceManager.GetString("Daemon_UsingDefaultDownloadLocation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Using daemon download url from {0} environment variable.
        /// </summary>
        internal static string Daemon_UsingDownloadUrlFromEnvVar {
            get {
                return ResourceManager.GetString("Daemon_UsingDownloadUrlFromEnvVar", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Daemon version: {0}.
        /// </summary>
        internal static string Daemon_Version {
            get {
                return ResourceManager.GetString("Daemon_Version", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error occurred configuring the daemon: {0}.
        /// </summary>
        internal static string ERROR_ConfiguringDaemon {
            get {
                return ResourceManager.GetString("ERROR_ConfiguringDaemon", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error occurred initializing the daemon package: {0}.
        /// </summary>
        internal static string ERROR_InitializingDaemon {
            get {
                return ResourceManager.GetString("ERROR_InitializingDaemon", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error occurred installing the daemon: {0}.
        /// </summary>
        internal static string ERROR_InstallingDaemon {
            get {
                return ResourceManager.GetString("ERROR_InstallingDaemon", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error stopping the daemon: {0}.
        /// </summary>
        internal static string ERROR_StoppingDaemon {
            get {
                return ResourceManager.GetString("ERROR_StoppingDaemon", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified argument is of a valid type..
        /// </summary>
        internal static string InvalidInfoBarInstance {
            get {
                return ResourceManager.GetString("InvalidInfoBarInstance", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid VisualStudio version, expecting &apos;14.0&apos; or &apos;15.0&apos; got &apos;{0}&apos;..
        /// </summary>
        internal static string InvalidVisualStudioVersion {
            get {
                return ResourceManager.GetString("InvalidVisualStudioVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cancelling the job &quot;{0}&quot; started at {1}.
        /// </summary>
        internal static string JobRunner_CancellingJob {
            get {
                return ResourceManager.GetString("JobRunner_CancellingJob", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error running job &quot;{0}&quot; started at {1}. Error message: {2}.
        /// </summary>
        internal static string JobRunner_ExecutionError {
            get {
                return ResourceManager.GetString("JobRunner_ExecutionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished job &quot;{0}&quot; started at {1}. Elapsed time: {2}ms.
        /// </summary>
        internal static string JobRunner_FinishedJob {
            get {
                return ResourceManager.GetString("JobRunner_FinishedJob", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to re-analyze open documents.
        /// </summary>
        internal static string JobRunner_JobDescription_ReaanalyzeOpenDocs {
            get {
                return ResourceManager.GetString("JobRunner_JobDescription_ReaanalyzeOpenDocs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting job: &quot;{0}&quot;. Time: {1}.
        /// </summary>
        internal static string JobRunner_StartingJob {
            get {
                return ResourceManager.GetString("JobRunner_StartingJob", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connected: checking for notifications.
        /// </summary>
        internal static string Notifications_Connected {
            get {
                return ResourceManager.GetString("Notifications_Connected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Notifications: error occurred: {0}.
        /// </summary>
        internal static string Notifications_ERROR {
            get {
                return ResourceManager.GetString("Notifications_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished initializing the notifications package.
        /// </summary>
        internal static string Notifications_InitializationComplete {
            get {
                return ResourceManager.GetString("Notifications_InitializationComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initializing the notifications package....
        /// </summary>
        internal static string Notifications_Initializing {
            get {
                return ResourceManager.GetString("Notifications_Initializing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loading notifications settings....
        /// </summary>
        internal static string Notifications_LoadingSettings {
            get {
                return ResourceManager.GetString("Notifications_LoadingSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Not connected: not checking for notifications.
        /// </summary>
        internal static string Notifications_NotConnected {
            get {
                return ResourceManager.GetString("Notifications_NotConnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Saving notifications settings....
        /// </summary>
        internal static string Notifications_SavingSettings {
            get {
                return ResourceManager.GetString("Notifications_SavingSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SonarLint package: error occurred: {0}.
        /// </summary>
        internal static string SL_ERROR {
            get {
                return ResourceManager.GetString("SL_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finishing initializing the SonarLint package.
        /// </summary>
        internal static string SL_InitializationComplete {
            get {
                return ResourceManager.GetString("SL_InitializationComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initializing the SonarLint package....
        /// </summary>
        internal static string SL_Initializing {
            get {
                return ResourceManager.GetString("SL_Initializing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Telemetry: error occurred: {0}.
        /// </summary>
        internal static string Telemetry_ERROR {
            get {
                return ResourceManager.GetString("Telemetry_ERROR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished initializing the telemetry package....
        /// </summary>
        internal static string Telemetry_InitializationComplete {
            get {
                return ResourceManager.GetString("Telemetry_InitializationComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initializing the telemetry package....
        /// </summary>
        internal static string Telemetry_Initializing {
            get {
                return ResourceManager.GetString("Telemetry_Initializing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error opening settings file &quot;{0}&quot;:
        ///  Error: {1}.
        /// </summary>
        internal static string ToolsOptions_ErrorOpeningSettingsFile {
            get {
                return ResourceManager.GetString("ToolsOptions_ErrorOpeningSettingsFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;sonarlint_product&quot;: &quot;SonarLint Visual Studio&quot;,
        ///  &quot;sonarlint_version&quot;: &quot;4.12.0.0&quot;,
        ///  &quot;ide_version&quot;: &quot;16.0.29123.89&quot;,
        ///  &quot;days_since_installation&quot;: 1,
        ///  &quot;days_of_use&quot;: 2,
        ///  &quot;connected_mode_used&quot;: false,
        ///  &quot;connected_mode_sonarcloud&quot;: false,
        ///  &quot;install_time&quot;: &quot;2019-08-05T10:03:25.923+00:00&quot;,
        ///  &quot;system_time&quot;: &quot;2019-08-06T11:31:00.142+01:00&quot;,
        ///  &quot;analyses&quot;: [
        ///    {
        ///      &quot;language&quot;: &quot;cs&quot;
        ///    },
        ///    {
        ///      &quot;language&quot;: &quot;js&quot;
        ///    },
        ///    {
        ///      &quot;language&quot;: &quot;cpp&quot;
        ///    },
        ///    {
        ///      &quot;language&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ToolsOptions_ExampleJson {
            get {
                return ResourceManager.GetString("ToolsOptions_ExampleJson", resourceCulture);
            }
        }
    }
}
