CFamily plugin pre-processor
----------------------------

Ultimately, we want to embed multiple artefacts in the SLVS VSIX:
1) the subprocess.exe,
2) the "LICENSE_THIRD_PARTY.txt" file, and
3) a file that contains all of rules metadata.

Artefacts (1) and (2) exist somewhere in the jar.
However, artefact (3) does not. Instead, there are some well-known json files and hundreds of per-rule files. We need to find and load all of these files and generate artefact (3).

Format of artefact (3)
----------------------
TODO


Integration with the rest of the build
--------------------------------------
The application is a standalone exe that is called as part of the main solution build.

-> the solution needs to reference the build tools.
