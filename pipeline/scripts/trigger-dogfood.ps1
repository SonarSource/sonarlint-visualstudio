Write-Host "Current branch: $env:BUILD_SOURCEBRANCH"
Write-Host "Trigger dogfood release flag; $env:TRIGGERDOGFOODRELEASEIFMASTERBRANCH"

if ("$env:BUILD_SOURCEBRANCH" -eq "refs/heads/master"){

    if ("$env:TRIGGERDOGFOODRELEASEIFMASTERBRANCH" -eq "true"){
        Write-Host "Building master and trigger flag is true. Setting tag to trigger dogfood release pipeline."
        Write-Host "##vso[build.addbuildtag]TriggerDogfoodVsixUpdate"
    }
    else{
        Write-Host "Building master but trigger flag is false. Tag to trigger dogfood release pipeline will not be set."
        Write-Host "##vso[build.addbuildtag]DoNot_TriggerDogfoodVsixUpdate_TriggerFlagIsFalse"
    }
}
else{
    Write-Host "Current branch is not master. Tag to trigger dogfood release pipeline will not be set."
    Write-Host "##vso[build.addbuildtag]BuildTag DoNot_TriggerDogfoodVsixUpdate_NotMaster"
}
