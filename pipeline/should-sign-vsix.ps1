 $ErrorView = 'NormalView'


        Write-Host "Current branch: $(Build.SourceBranch)"

        Write-Host "SignVsixIfMasterBranch flag; $(SignVsixIfMasterBranch)"

        Write-Host "ForceSignVsix flag; $(ForceSignVsix)"


        if ("$(ForceSignVsix)" -eq "true"){
         Write-Host "ForceSignVsix is set to true. Signing vsix."
         $shouldSign = $true
        }

        else {

        Write-Host "ForceSignVsix is set to false. Checking if master branch..."


        if ("$(Build.SourceBranch)" -eq "refs/heads/master"){

            if ("$(SignVsixIfMasterBranch)" -eq "true"){
                Write-Host "Building master and trigger flag is true. Signing vsix."
                $shouldSign = $true
            }
            else{
                Write-Host "Building master but trigger flag is false. Vsix will not be signed."
                $shouldSign = $false
            }
        }

        else{
            Write-Host "Current branch is not master. Vsix will not be signed."
             $shouldSign = $false
        }

        }


        Write-Host "Setting SHOULD_SIGN flag:" $shouldSign


        Write-Host "##vso[task.setvariable variable=SHOULD_SIGN;]$shouldSign"


        Write-Host "SHOULD_SIGN flag set:" $shouldSign