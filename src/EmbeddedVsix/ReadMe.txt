SonarLint.AdditionalFiles VSIX
------------------------------

This VSIX is never installed separately, and should never be visible to the end user in the extensions list in the IDE.

It is embedded in the main SLVS VSIX. It only exists to work round issues with the VSSDK tooling, which fails to build
the SLVS VSIX if the additional files are embedded directly in that VSIX ("out of memory" error if there are too many files).

The files are dynamically added at build time. This doesn't affect how the VSIX is built, and is done to stop the Solution Explorer
from freezing due to large number of files that are included.