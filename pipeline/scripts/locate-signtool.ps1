# Search for signtool.exe

$searchRoot = ${env:ProgramFiles(x86)} + 'Microsoft SDKs\ClickOnce\SignTool\'

$exeName = 'signtool.exe'

$signtool = 'C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe'


if (!$signtool){
	throw 'Unable to find ' + $exeName + ' under ' + $searchRoot
}


Write-Host 'Sign tool location: ' $signtool.FullName


Write-Host "##vso[task.setvariable variable=SIGNTOOL_PATH;]$signtool"