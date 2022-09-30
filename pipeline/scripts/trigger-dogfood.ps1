param($SourceBranch, $TriggerDogfoodReleaseIfMasterBranch)

Write-Host "Current branch: $SourceBranch"
Write-Host "Trigger dogfood release flag; $TriggerDogfoodReleaseIfMasterBranch"

if ("$SourceBranch" -eq "refs/heads/master"){

    if ("$TriggerDogfoodReleaseIfMasterBranch" -eq "true"){
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
