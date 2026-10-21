$ErrorActionPreference = 'Stop'
$pauseRoot = Split-Path $PSScriptRoot
$pauseCode = "using System;`nusing System.Collections.Generic;`nusing System.Reflection;`nusing System.Runtime.InteropServices;`nusing UnityEngine;`nusing UnityEngine.SceneManagement;`nusing UnityEngine.EventSystems;`n#pragma warning disable 0649`n"
foreach ($pauseSource in @('Scripts/Yandex/YandexSdk.cs', 'Scripts/Yandex/YandexBridge.cs', 'Scripts/Yandex/YandexLeaderboard.cs', 'Scripts/Yandex/SaveSnapshot.cs', 'Scripts/GameManagers/SaveKeys.cs', 'Scripts/GameManagers/LevelProgress.cs', 'Scripts/Audio/AudioManager.cs', 'Scripts/Car/ClickDetector/ClickDetector.cs')) {
    $pauseText = Get-Content -Raw -LiteralPath (Join-Path $pauseRoot $pauseSource)
    $pauseCode += [regex]::Replace($pauseText, '(?m)^using [^;]+;\r?\n', '') + "`n"
}
$pauseCode += Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'YandexPauseRegression.cs.txt')
Add-Type -TypeDefinition $pauseCode
[YandexPauseRegression]::Run()
Write-Output "PASS: $([YandexPauseRegression]::Passed) assertions against production pause/audio/input code (Unity engine APIs stubbed)."
