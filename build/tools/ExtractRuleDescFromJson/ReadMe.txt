ExtractRuleDescFromJson
======================

This is a build-time tool that extract the rule description from the rule metadata json file.

Assumptions:
* the analyzer JAR files have already been unloaded and the rule json extracted (one file per language)


Processing steps
----------------
1. Load the json file
2. Extract the HTML description for each rule
3. ?if necessary, fix up the HTML (e.g. remove &nbsp;)
4. Save the HTML to the destination directory
