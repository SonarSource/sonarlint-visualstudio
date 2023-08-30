SLVS - third-party assemblies folder
------------------------------------

Most of the assemblies we need are contained in the VS SDK or other NuGet package, so we just need
to reference the appropriate NuGet package.

However, there are a few VS assemblies that are not available in the VS SDK or other NuGet package
Also, we need to reference different versions of these assemblies for different versions of VS (*).

To work round this, we have copies of the required assemblies in the repo under the "third-parties"
folder. The projects that need to reference these assemblies do so add them as normal MSBuild
<Reference>s, rather than as <PackageReferences>.

The logic for adding the references and working out which version of the third-party assembly to use
is centralised in the following properties file:

   SonarLint.VSSpecificAssemblies.props

If project needs to reference one of these assemblies, all it needs to do is add the appropriate property

e.g. 

  <PropertyGroup>
    <RequiresLanguageServices>true</RequiresLanguageServices>
  </PropertyGroup>





(*) why? Mainly internal organisation reasons at Microsoft e.g. the TeamExplorer assemblies are not owned
by the core VS platform team who own the VS SDK, so those assemblies are not included in the VS SDK.
Also, the owning team doesn't follow the same binary backwards-compatibility approach that the platform
VS SDK team does, so we can't just reference e.g. VS2019 version of the TeamExplorer assemblies and
have it work for VS2022 as well.