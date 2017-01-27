$ErrorActionPreference = "Stop"

function testExitCode(){
    If($LASTEXITCODE -ne 0) {
        write-host -f green "lastexitcode: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
write-host -f green  "Node labels: $env:NODE_LABELS"
if ($env:NODE_LABELS.contains("vs2015")) {
  write-host -f green  "Starting vs2015 build"
  & .\build\build15.ps1
  testExitCode
}

if ($env:NODE_LABELS.contains("vs2017")) {
  write-host -f green  "Starting vs2017 build"
  & .\build\build17.ps1
  testExitCode
}

