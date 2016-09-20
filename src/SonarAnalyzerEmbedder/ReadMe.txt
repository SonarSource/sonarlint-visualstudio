This is a fake project to reference a version of SonarAnalyzer that gets embedded in the VSIX.
The VSIX project references this one to force the building of it, but the output is "CopyLocal=false",
so no DLL is actually being copied anywhere.