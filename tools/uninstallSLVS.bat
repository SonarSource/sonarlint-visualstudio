::    Run this from a VS command prompt (any version)
::    It will uninstall SLVS for VS2015, 2017 and 2019
::    from both the main and experimental hives

echo Uninstalling SLVS for VS2015...
vsixinstaller /q /u:SonarLint.8c442ec8-0208-4840-b83f-acd24f41acf7

echo Uninstalling SLVS for VS2017...
vsixinstaller /q /u:SonarLint.36871a7b-4853-481f-bb52-1835a874e81b

echo Uninstalling SLVS for VS2019...
vsixinstaller /q /u:SonarLint.b986f788-6a16-4a3a-a68b-c757f6b1b7d5

echo Uninstalling SLVS for VS2015 (experimental hive)...
vsixinstaller /q /u:SonarLint.8c442ec8-0208-4840-b83f-acd24f41acf7 /rootSuffix:Exp

echo Uninstalling SLVS for VS2017 (experimental hive)...
vsixinstaller /q /u:SonarLint.36871a7b-4853-481f-bb52-1835a874e81b /rootSuffix:Exp

echo Uninstalling SLVS for VS2019 (experimental hive)...
vsixinstaller /q /u:SonarLint.b986f788-6a16-4a3a-a68b-c757f6b1b7d5 /rootSuffix:Exp

