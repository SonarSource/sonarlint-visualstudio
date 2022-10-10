# Search for signtool.exe

$searchRoot = ${env:ProgramFiles(x86)} + '\Windows Kits\10\bin\10*'

$exeName = 'signtool.exe'

$signtool = Get-ChildItem -Path $searchRoot -Filter $exeName -Recurse -ErrorAction SilentlyContinue -Force | Select -Last 1


if (!$signtool){
	throw 'Unable to find ' + $exeName + ' under ' + $searchRoot
}


Write-Host 'Sign tool location: ' $signtool.FullName


Write-Host "##vso[task.setvariable variable=SIGNTOOL_PATH;]$signtool"