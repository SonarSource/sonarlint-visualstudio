SonarLint.AdditionalFiles VSIX
------------------------------

This VSIX is never installed separately, and should never be visible to the end user in the extensions list in the IDE.

It is embedded in the main SLVS VSIX. It only exists to work round issues with the VSSDK tooling, which fails to build
the SLVS VSIX if the additional files are embedded directly in that VSIX ("out of memory" error if there are too many files).

The files are dynamically added at build time. This doesn't affect how the VSIX is built, and is done to stop the Solution Explorer
from freezing due to large number of files that are included.


This project has a couple of unusual characteristics that affect how it is built and used:

* the contents of this project should be very stable; we should only need to rebuild it when the referenced version of the SonarJS plugin changes.
* building this project can be slow (it creates a VSIX containing thousands of files, which is then installed in the Experimental hive).

These characteristics mean we want to build the project as infrequently as possible.

This in turn means that this project should NOT reference any other project in the solution since they all change
more frequently than the embedded analyzer version.

However, the other projects do need to know where some of the files are shipped in this VSIX are installed
e.g. the eslint-bridge server. To avoid having to create references between the projects, we are exporting
simple properties via MEF using specific contract names.