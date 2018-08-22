# Change Log (Release Notes)

All _notable_ changes to this project will be documented in this file (`CHANGELOG.md`).

Contributors to this file, please follow the guidelines on [keepachangelog.com](http://keepachangelog.com/).

For reference, the possible headings are:

* **New Features** for new features.
* **Improvements** for changes in existing functionality.
* **Bugs** for any bug fixes.
* **External Contributors** to list contributors outside of SonarSource SA.
* **Notes**


## [Unreleased](https://github.com/SonarSource/sonarlint-visualstudio/compare/4.3.0.3718...HEAD)


## [4.3](https://github.com/SonarSource/sonarlint-visualstudio/compare/4.2.0.3692...4.3.0.3718)

### Improvements
* [#656](https://github.com/SonarSource/sonarlint-visualstudio/issues/711) - Update SonarAnalyzer to 7.4 


## [4.2](https://github.com/SonarSource/sonarlint-visualstudio/compare/4.1.0.3539...4.2.0.3692)

### Bug fixes
* [#642](https://github.com/SonarSource/sonarlint-visualstudio/issues/642) - New binding: double-clicking on the project in the list does nothing
* [#647](https://github.com/SonarSource/sonarlint-visualstudio/issues/647) - SonarLint for VS 2017 4.0.0.3479 likely caused 6 seconds of unresponsiveness
* [#648](https://github.com/SonarSource/sonarlint-visualstudio/issues/648) - Unable to collect C/C++ configuration: System.Reflection.TargetInvocationException
* [#661](https://github.com/SonarSource/sonarlint-visualstudio/issues/661) - SonarLint crashes VS when opening a legacy-bound solution
* [#662](https://github.com/SonarSource/sonarlint-visualstudio/issues/662) - Do not throw Linq exception when SonarC# is not installed on SonarQube
* [#667](https://github.com/SonarSource/sonarlint-visualstudio/issues/667) - Loading a C++ file outside of a project generates an error
* [#676](https://github.com/SonarSource/sonarlint-visualstudio/issues/676) - [C++] Opening a header file in External Dependencies throws a NullReferenceException
* [#686](https://github.com/SonarSource/sonarlint-visualstudio/issues/686) - Connection information is ignored
* [#688](https://github.com/SonarSource/sonarlint-visualstudio/issues/688) - [Client] api/qualityprofiles/changelog needs to provide organisation when fetching custom profile on SonarCloud
* [#689](https://github.com/SonarSource/sonarlint-visualstudio/issues/689) - [Client] LoggingHttpClientHandler is disposed when a bound solution is closed
* [#691](https://github.com/SonarSource/sonarlint-visualstudio/issues/691) - [Daemon] "Unsupported content type" message in output pane even for file types that are handled

### Improvements
* [#274](https://github.com/SonarSource/sonarlint-visualstudio/issues/274) - Update usage of deprecated API
* [#595](https://github.com/SonarSource/sonarlint-visualstudio/issues/595) - Update SonarQube Client to support new APIs
* [#666](https://github.com/SonarSource/sonarlint-visualstudio/issues/666) - Use api/issues/search to retrieve suppressed issues for SQ >= 7.2
* [#679](https://github.com/SonarSource/sonarlint-visualstudio/issues/679) - Convert SonarLintNotificationsPackage to be asynchronous
* [#681](https://github.com/SonarSource/sonarlint-visualstudio/issues/681) - Convert SonarLintTelemetryPackage to be asynchronous
* [#704](https://github.com/SonarSource/sonarlint-visualstudio/issues/704) - Embed SonarC#/SonarVB 7.3.1

## [4.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/4.0.0.3479...4.1.0.3539)

### Improvements
* [#656](https://github.com/SonarSource/sonarlint-visualstudio/issues/656) - Update SonarAnalyzer to 7.2 

### Bug fixes
* [#653](https://github.com/SonarSource/sonarlint-visualstudio/issues/653) - Fix telemetry dates by handling cultures


## [4.0](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.10.0.3095...4.0.0.3479)

### New features
* [#188](https://github.com/SonarSource/sonarlint-visualstudio/issues/188) - Add a command to unbind a project

### Improvements
* [#400](https://github.com/SonarSource/sonarlint-visualstudio/issues/400) - Updating the bound project (SonarQube Team Explorer page) should refresh suppressed issues
* [#539](https://github.com/SonarSource/sonarlint-visualstudio/issues/539) - Use AsyncPackage class instead of Package to load in the background
* [#547](https://github.com/SonarSource/sonarlint-visualstudio/issues/547) - Clicking Update on SonarQube Team Explorer page should force synchronization of Quality Profiles
* [#638](https://github.com/SonarSource/sonarlint-visualstudio/issues/638) - Update daemon to version 3.4.0.1536
* [#640](https://github.com/SonarSource/sonarlint-visualstudio/issues/640) - Update SonarAnalyzer to 7.1.0.5212

### Bug fixes
* [#594](https://github.com/SonarSource/sonarlint-visualstudio/issues/594) - InvalidOperationException thrown


## [3.10](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.9.0.3021...3.10.0.3095)

### New Feature
* [#531](https://github.com/SonarSource/sonarlint-visualstudio/issues/531) - Embed SonarC# and SonarVB analyzers 6.8.1


## [3.9](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.8.1.2823...3.9.0.3021)

### New Features
* [#238](https://github.com/SonarSource/sonarlint-visualstudio/issues/238) - Add the support of Vue.js single file components (.vue files)
* [#495](https://github.com/SonarSource/sonarlint-visualstudio/issues/495) - Add the support of jsx files

### Improvements
* [#248](https://github.com/SonarSource/sonarlint-visualstudio/issues/248) - Display issue description  in a tooltip for SonarLint daemon issues
* [#288](https://github.com/SonarSource/sonarlint-visualstudio/issues/288) - Include local time in telemetry data
* [#441](https://github.com/SonarSource/sonarlint-visualstudio/issues/441) - Include installation time in telemetry data
* [#476](https://github.com/SonarSource/sonarlint-visualstudio/issues/476) - Display a message to users with VS2015 under update 3
* [#500](https://github.com/SonarSource/sonarlint-visualstudio/issues/500) - Do not allow to install new major version SonarC# / SonarVB on old VS
* [#515](https://github.com/SonarSource/sonarlint-visualstudio/issues/515) - SonarLint should embed Daemon version 3.1.0.1376

### Bugs
* [#424](https://github.com/SonarSource/sonarlint-visualstudio/issues/424) - Unable to sync custom Quality Profile from SonarCloud
* [#470](https://github.com/SonarSource/sonarlint-visualstudio/issues/470) - Do not store data in the user's roaming profile
* [#473](https://github.com/SonarSource/sonarlint-visualstudio/issues/473) - Cannot update bindings when SonarC# 6.7 is installed on SonarQube
* [#480](https://github.com/SonarSource/sonarlint-visualstudio/issues/480) - Help link is not correct for JS/C/C++
* [#494](https://github.com/SonarSource/sonarlint-visualstudio/issues/494) - Category of JS/C/C++ doesn't display the right severity

### Notes
What about:
* SonarLint for Visual Studio versions 4.0+ will no longer support Visual Studio older than VS 2015 Update 3.
Please update to Visual Studio 2015 Update 3+ or Visual Studio 2017 to benefit from new features.


## [3.8.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.8.0.2728...3.8.1.2823)

### Bugs
* [#458](https://github.com/SonarSource/sonarlint-visualstudio/issues/458) - Unify first day ping with other SonarLint variants
* [#464](https://github.com/SonarSource/sonarlint-visualstudio/issues/464) - Connected mode always fetches the default quality profile for SQ6.6+

### Improvements
* [#467](https://github.com/SonarSource/sonarlint-visualstudio/issues/467) - Embed SonarC#/SonarVB 6.7.1


## [3.8](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.7.0.2645...3.8.0.2728)

### New Features
* [#151](https://github.com/SonarSource/sonarlint-visualstudio/issues/151) - Enable connected mode for .Net Core projects

### Improvements
* [#435](https://github.com/SonarSource/sonarlint-visualstudio/issues/435) - Upgrade additional analyzers with the latest versions
* [#451](https://github.com/SonarSource/sonarlint-visualstudio/issues/451) - Update SonarLint description for the marketplace

### Bugs
* [#440](https://github.com/SonarSource/sonarlint-visualstudio/issues/440) - OptOut button does not work


## [3.7](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.6.0.2584...3.7.0.2645)

### Bugs
* [#395](https://github.com/SonarSource/sonarlint-visualstudio/issues/395) - Binding the solution to SonarQube before all projects have finished to load result in weird error
* [#413](https://github.com/SonarSource/sonarlint-visualstudio/issues/413) - Opening a solution with duplicate solution folder names crashes VS

### Improvements
* [#347](https://github.com/SonarSource/sonarlint-visualstudio/issues/347) - "Connect" option should be available only after a solution is loaded
* [#422](https://github.com/SonarSource/sonarlint-visualstudio/issues/422) - Embed SonarC# and SonarVB v6.6 RC1


## [3.6](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.5.0.1803...3.6.0.2584)

### New Features
* [#267](https://github.com/SonarSource/sonarlint-visualstudio/issues/267) - SonarLint should not show issues marked as FP or Won't Fix

### Improvements
* [#257](https://github.com/SonarSource/sonarlint-visualstudio/issues/257) - Update the Marketplace Icon
* [#378](https://github.com/SonarSource/sonarlint-visualstudio/issues/378) - Embed SonarC# 6.5 and SonarVB 3.1
* [#278](https://github.com/SonarSource/sonarlint-visualstudio/issues/278) - Unify the telemetry behavior with other SonarLint IDEs

### Bugs
* [#380](https://github.com/SonarSource/sonarlint-visualstudio/issues/380) - Typo on SonarLint for VS 2017 marketplace page
* [#383](https://github.com/SonarSource/sonarlint-visualstudio/issues/383) - Binding to a project on SonarCloud always prompt for organization


## [3.5](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.4.0.1732...3.5.0.1803)

### Improvements
* [#252](https://github.com/SonarSource/sonarlint-visualstudio/issues/252) - Embed SonarC# 6.4

### Bugs
* [#244](https://github.com/SonarSource/sonarlint-visualstudio/issues/244) - Spelling error in additional languages download dialog
* [#243](https://github.com/SonarSource/sonarlint-visualstudio/issues/243) - The 'SonalLintDaemonPackage' package did not load correctly.


## [3.4](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.3.0.1663...3.4.0.1732)

### New Features
* Provide support for C/C++ in standalone

### Improvements
* [#239](https://github.com/SonarSource/sonarlint-visualstudio/issues/239) - Embed SonarC# 6.3
* [#235](https://github.com/SonarSource/sonarlint-visualstudio/issues/235) - Update SonarJS to version 3.1
* [#234](https://github.com/SonarSource/sonarlint-visualstudio/issues/234) - Include Visual Studio version in telemetry data

### Bugs
* [#228](https://github.com/SonarSource/sonarlint-visualstudio/issues/228) - VisualStudio Enterprise considers the extension is slowing down the startup


## [3.3](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.2.1.1639...3.3.0.1663)

### Improvements
* [#223](https://github.com/SonarSource/sonarlint-visualstudio/issues/223) - Embed SonarC# 6.2
* [#202](https://github.com/SonarSource/sonarlint-visualstudio/issues/202) - Update SonarLint extension description


## [3.2.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.2.0.1601...3.2.1.1639)

### Bugs
* [#216](https://github.com/SonarSource/sonarlint-visualstudio/issues/216) - Fix broken analyzer reference


## [3.2](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.1.1.1583...3.2.0.1601)

### Improvements
* [#211](https://github.com/SonarSource/sonarlint-visualstudio/issues/211) - Embed SonarC# 6.1


## [3.1.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.1.0.1578...3.1.1.1583)

### Bugs
* [#207](https://github.com/SonarSource/sonarlint-visualstudio/issues/207) - Fix a connectivity problem with SonarCloud


## [3.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/3.0.0.1569...3.1.0.1578)

### Improvements
* [#204](https://github.com/SonarSource/sonarlint-visualstudio/issues/204) - Embed SonarC# 6.0


## [3.0](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.13.0.1333...3.0.0.1569)

### New Features
* [#164](https://github.com/SonarSource/sonarlint-visualstudio/issues/164) - Provide support for Javascript in standalone
* [#173](https://github.com/SonarSource/sonarlint-visualstudio/issues/173) - Add support for SonarQube organizations in connected mode
* [#142](https://github.com/SonarSource/sonarlint-visualstudio/issues/142) - Add telemetry

### Improvements
* [#199](https://github.com/SonarSource/sonarlint-visualstudio/issues/199) - Embed SonarC# 5.11
* [#156](https://github.com/SonarSource/sonarlint-visualstudio/issues/156) - SonarLint is not listed in the Help/About of Visual Studio
* [#136](https://github.com/SonarSource/sonarlint-visualstudio/issues/136) - Provide a digital signature to the extension


## [2.13](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.12.0.1175...2.13.0.1333)

### Improvements
* [#154](https://github.com/SonarSource/sonarlint-visualstudio/issues/154) - Embed SonarC# 5.10


## [2.12](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.11.0.1102...2.12.0.1175)

### Improvements
* [#135](https://github.com/SonarSource/sonarlint-visualstudio/issues/135) - Embed SonarC# 5.9

### Bugs
* [#132](https://github.com/SonarSource/sonarlint-visualstudio/issues/132) - SonarLint for VS2017 crashes when VS2015 is not installed


## [2.11](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.10...2.11.0.1102)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13700)


## [2.10](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.9-fixed...2.10)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13664)


## [2.9](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.8.1...2.9-fixed)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13464)


## [2.8.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.8...2.8.1)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13509)


## [2.8](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.7...2.8)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13408)


## [2.7](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.6...2.7)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13388)


## [2.6](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.5...2.6)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13240)


## [2.5](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.4...2.5)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13345)


## [2.4](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.3...2.4)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13312)


## [2.3](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.2.1...2.3)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13278)


## [2.2.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.2...2.2.1)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13234)


## [2.2](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.1...2.2)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=13034)


## [2.1](https://github.com/SonarSource/sonarlint-visualstudio/compare/2.0...2.1)

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=12988)


## 2.0

[JIRA Release Note](https://jira.sonarsource.com/jira/secure/ReleaseNote.jspa?projectId=11242&version=12987)
