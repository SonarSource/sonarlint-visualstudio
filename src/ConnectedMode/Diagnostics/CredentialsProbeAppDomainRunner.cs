/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Diagnostics;

// Runs in a throwaway child AppDomain (see CredentialsDependencyProbe) so any assembly it actually loads is gone
// when that domain unloads, instead of lingering in VS's main AppDomain. Must derive from MarshalByRefObject to
// be usable via CreateInstanceAndUnwrap, and results cross the domain boundary as plain strings since ILogger
// isn't available in the child domain.
public class CredentialsProbeAppDomainRunner : MarshalByRefObject
{
    private const string DiagnosticsNamespace = "SonarLint.VisualStudio.Integration.Diagnostics";
    private const string DiagnosticsUriScheme = "https";
    private const string DiagnosticsUriHost = "sonarlint-credentials-diagnostics.invalid";

    private readonly List<string> log = [];

    public string[] RunIsolatedProbes()
    {
        Probe(nameof(ProbeExactReferencedAssemblyBinds), ProbeExactReferencedAssemblyBinds);
        Probe(nameof(ProbeTargetUriConstruction), ProbeTargetUriConstruction);
        Probe(nameof(ProbeCredentialConstruction), ProbeCredentialConstruction);
        Probe(nameof(ProbeCredentialStoreInterfaceTypeLoad), ProbeCredentialStoreInterfaceTypeLoad);
        Probe(nameof(ProbeSecretStoreConstruction), ProbeSecretStoreConstruction);
        Probe(nameof(ProbeSecretStoreRead), ProbeSecretStoreRead);
        Probe(nameof(ProbeCredentialStoreWrapperConstruction), ProbeCredentialStoreWrapperConstruction);
        Probe(nameof(ProbeDefaultBindingCredentialsLoaderConstruction), ProbeDefaultBindingCredentialsLoaderConstruction);

        return log.ToArray();
    }

    private void Probe(string stepName, Action step)
    {
        try
        {
            step();
            log.Add($"Probe OK: {stepName}");
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaderExceptions = string.Join(" | ", Array.ConvertAll(ex.LoaderExceptions ?? Array.Empty<Exception>(), x => x?.ToString()));
            log.Add($"Probe FAILED [{nameof(ReflectionTypeLoadException)}] {stepName}: {ex.Message}. LoaderExceptions: {loaderExceptions}");
        }
        // DllNotFoundException and EntryPointNotFoundException both derive from TypeLoadException - must be caught first.
        catch (DllNotFoundException ex)
        {
            log.Add($"Probe FAILED [{nameof(DllNotFoundException)}] {stepName}: {ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            log.Add($"Probe FAILED [{nameof(EntryPointNotFoundException)}] {stepName}: {ex.Message}");
        }
        catch (TypeLoadException ex)
        {
            log.Add($"Probe FAILED [{nameof(TypeLoadException)}] {stepName}: TypeName={ex.TypeName}, {ex.Message}");
        }
        catch (TypeInitializationException ex)
        {
            log.Add($"Probe FAILED [{nameof(TypeInitializationException)}] {stepName}: TypeName={ex.TypeName}, Inner={ex.InnerException}");
        }
        catch (FileNotFoundException ex)
        {
            log.Add($"Probe FAILED [{nameof(FileNotFoundException)}] {stepName}: FileName={ex.FileName}, {ex.Message}. FusionLog={ex.FusionLog}");
        }
        catch (FileLoadException ex)
        {
            log.Add($"Probe FAILED [{nameof(FileLoadException)}] {stepName}: FileName={ex.FileName}, {ex.Message}. FusionLog={ex.FusionLog}");
        }
        catch (BadImageFormatException ex)
        {
            log.Add($"Probe FAILED [{nameof(BadImageFormatException)}] {stepName}: FileName={ex.FileName}, {ex.Message}");
        }
        catch (MissingMethodException ex)
        {
            log.Add($"Probe FAILED [{nameof(MissingMethodException)}] {stepName}: {ex.Message}");
        }
        catch (MissingMemberException ex)
        {
            log.Add($"Probe FAILED [{nameof(MissingMemberException)}] {stepName}: {ex.Message}");
        }
        catch (MethodAccessException ex)
        {
            log.Add($"Probe FAILED [{nameof(MethodAccessException)}] {stepName}: {ex.Message}");
        }
        catch (Win32Exception ex)
        {
            log.Add($"Probe FAILED [{nameof(Win32Exception)}] {stepName}: NativeErrorCode={ex.NativeErrorCode}, {ex.Message}");
        }
        catch (Exception ex)
        {
            log.Add($"Probe FAILED [{ex.GetType().FullName}] {stepName}: {ex}");
        }
    }

    // Binds by the exact AssemblyName (version + PublicKeyToken) this assembly's metadata expects, not
    // Assembly.Load(simpleName) - an unqualified load would mask a strong-name mismatch instead of reproducing it.
    private void ProbeExactReferencedAssemblyBinds()
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        foreach (var referenced in thisAssembly.GetReferencedAssemblies().Where(x => CredentialsDependencyProbe.IsAlmRelated(x.Name)))
        {
            Probe($"{nameof(ProbeExactReferencedAssemblyBinds)}:{referenced.FullName}", () => LoadExact(referenced));
        }
    }

    private void LoadExact(AssemblyName assemblyName)
    {
        var loaded = Assembly.Load(assemblyName);
        log.Add($"Exact bind OK for {assemblyName.FullName} -> loaded {loaded.FullName} from {loaded.Location}");
    }

    private void ProbeTargetUriConstruction()
    {
        var targetUri = new TargetUri($"{DiagnosticsUriScheme}://{DiagnosticsUriHost}/");
        log.Add($"Constructed TargetUri: {targetUri.ActualUri}");
    }

    private void ProbeCredentialConstruction()
    {
        var credential = new Credential("probe-token");
        log.Add($"Constructed Credential, username={credential.Username}");
    }

    private void ProbeCredentialStoreInterfaceTypeLoad()
    {
        log.Add($"Type loaded: {typeof(ICredentialStore)}");
        log.Add($"Type loaded: {typeof(ITokenStore)}");
    }

    private void ProbeSecretStoreConstruction()
    {
        var secretStore = new SecretStore(DiagnosticsNamespace);
        log.Add($"Constructed {secretStore.GetType().FullName}");
    }

    private void ProbeSecretStoreRead()
    {
        var secretStore = new SecretStore(DiagnosticsNamespace);
        var result = secretStore.ReadCredentials(new TargetUri($"{DiagnosticsUriScheme}://{DiagnosticsUriHost}/"));
        log.Add($"SecretStore.ReadCredentials completed, found={result != null}");
    }

    private void ProbeCredentialStoreWrapperConstruction()
    {
        var credentialStore = new CredentialStore(new CollectingLogger(log));
        log.Add($"Constructed {credentialStore.GetType().FullName}");
    }

    private void ProbeDefaultBindingCredentialsLoaderConstruction()
    {
        var collectingLogger = new CollectingLogger(log);
        var credentialStore = new CredentialStore(collectingLogger);
        var loader = new DefaultBindingCredentialsLoader(credentialStore, collectingLogger);
        log.Add($"Constructed {loader.GetType().FullName}, StoreType={loader.StoreType}");
    }

    // Minimal ILogger so CredentialStore/DefaultBindingCredentialsLoader can be constructed in here - the real
    // MEF-composed ILogger lives in the main AppDomain and isn't reachable from this one.
    private sealed class CollectingLogger(List<string> log) : ILogger
    {
        public void WriteLine(string messageFormat, params object[] args) => log.Add(Format(messageFormat, args));

        public void WriteLine(MessageLevelContext context, string messageFormat, params object[] args) => log.Add(Format(messageFormat, args));

        public void LogVerbose(string messageFormat, params object[] args) => log.Add(Format(messageFormat, args));

        public void LogVerbose(MessageLevelContext context, string messageFormat, params object[] args) => log.Add(Format(messageFormat, args));

        public ILogger ForContext(params string[] context) => this;

        public ILogger ForVerboseContext(params string[] context) => this;

        private static string Format(string messageFormat, object[] args) => args is { Length: > 0 } ? string.Format(messageFormat, args) : messageFormat;
    }
}
