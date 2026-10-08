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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.Diagnostics;

// Temporary diagnostic for the CREDTRACE investigation: the default credentials store MEF part sometimes doesn't
// show up in AggregatingSolutionBindingCredentialsLoader's ImportMany. Users who hit it can't repro it locally, so
// this ships in the VSIX and logs where the dependency chain actually breaks.
[Export(typeof(ICredentialsDependencyProbe))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class CredentialsDependencyProbe : ICredentialsDependencyProbe
{
    private readonly ILogger logger;

    [ImportingConstructor]
    public CredentialsDependencyProbe(ILogger logger)
    {
        this.logger = logger.ForVerboseContext(nameof(CredentialsDependencyProbe));
    }

    public void RunDiagnostics()
    {
        logger.WriteLine("[CREDTRACE] CredentialsDependencyProbe: starting dependency chain diagnostics");

        LogExtensionDirectoryAssemblyMetadata();
        LogAppDomainLoadedAssemblies();
        LogReferencedAssemblyIdentities();
        RunIsolatedLoadProbes();

        logger.WriteLine("[CREDTRACE] CredentialsDependencyProbe: diagnostics complete");
    }

    // AssemblyName.GetAssemblyName reads the PE header directly and never enters any load context (execution or
    // reflection-only), so it's genuinely footprint-free here. Unlike that, Assembly.ReflectionOnlyLoadFrom loads
    // permanently into the current AppDomain's reflection-only context until that domain unloads - so the
    // reflection-only cross-reference check runs from CredentialsProbeAppDomainRunner instead, not here.
    private void LogExtensionDirectoryAssemblyMetadata()
    {
        var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        logger.WriteLine($"[CREDTRACE] Extension directory: {extensionDirectory}");

        if (string.IsNullOrEmpty(extensionDirectory))
        {
            return;
        }

        foreach (var pattern in new[] { "Microsoft.Alm*.dll", "Microsoft.IdentityModel*.dll", "Microsoft.Vsts*.dll" })
        {
            foreach (var filePath in Directory.EnumerateFiles(extensionDirectory, pattern))
            {
                LogOnDiskAssemblyMetadata(filePath);
            }
        }
    }

    private void LogOnDiskAssemblyMetadata(string filePath)
    {
        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(filePath);
            logger.WriteLine($"[CREDTRACE] On-disk assembly {filePath}: {assemblyName.FullName}");
        }
        catch (Exception ex)
        {
            logger.WriteLine($"[CREDTRACE] Could not read assembly metadata for {filePath}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void LogAppDomainLoadedAssemblies()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName();
            if (IsAlmRelated(name.Name))
            {
                logger.WriteLine($"[CREDTRACE] Loaded in main AppDomain: {name.FullName} from {(assembly.IsDynamic ? "<dynamic>" : assembly.Location)}");
            }
        }
    }

    // Pure metadata read of this assembly's own AssemblyRef table - never loads or resolves anything.
    private void LogReferencedAssemblyIdentities()
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        foreach (var referenced in thisAssembly.GetReferencedAssemblies().Where(x => IsAlmRelated(x.Name)))
        {
            logger.WriteLine($"[CREDTRACE] {thisAssembly.GetName().Name} expects to bind: {referenced.FullName}");
        }
    }

    // Every probe that actually loads/constructs something runs in a throwaway child AppDomain, so a real bind
    // failure (or a successful load that would otherwise pin an unexpected identity) unloads with the domain
    // instead of lingering in VS's main AppDomain.
    private void RunIsolatedLoadProbes()
    {
        AppDomain childDomain = null;
        try
        {
            var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            childDomain = AppDomain.CreateDomain(nameof(CredentialsProbeAppDomainRunner), null, new AppDomainSetup { ApplicationBase = extensionDirectory });

            var runner = (CredentialsProbeAppDomainRunner)childDomain.CreateInstanceAndUnwrap(
                Assembly.GetExecutingAssembly().FullName,
                typeof(CredentialsProbeAppDomainRunner).FullName);

            foreach (var line in runner.RunIsolatedProbes())
            {
                logger.WriteLine($"[CREDTRACE] {line}");
            }
        }
        catch (Exception ex)
        {
            logger.WriteLine($"[CREDTRACE] Isolated probe AppDomain failed [{ex.GetType().FullName}]: {ex}");
        }
        finally
        {
            if (childDomain != null)
            {
                try
                {
                    AppDomain.Unload(childDomain);
                }
                catch (Exception ex)
                {
                    logger.WriteLine($"[CREDTRACE] Could not unload isolated probe AppDomain [{ex.GetType().FullName}]: {ex.Message}");
                }
            }
        }
    }

    internal static bool IsAlmRelated(string assemblyName) =>
        assemblyName.IndexOf("Alm", StringComparison.OrdinalIgnoreCase) >= 0
        || assemblyName.IndexOf("IdentityModel", StringComparison.OrdinalIgnoreCase) >= 0
        || assemblyName.IndexOf("Vsts", StringComparison.OrdinalIgnoreCase) >= 0;
}
