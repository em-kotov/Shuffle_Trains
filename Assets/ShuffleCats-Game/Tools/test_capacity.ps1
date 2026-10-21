$ErrorActionPreference = 'Stop'
$puzzleSources = @(
    'Scripts/SorterRegistrator/CatColor.cs',
    'Scripts/SorterRegistrator/SeatTracker.cs',
    'Scripts/SorterRegistrator/SorterRegistrator.cs',
    'Scripts/Car/SorterCar/PassengerTracker.cs',
    'Scripts/Car/SorterCar/StationOperator.cs',
    'Scripts/Car/SorterCar/SorterCar.cs',
    'Scripts/Station/Station.cs',
    'Scripts/Passenger/Passenger.cs'
)
$puzzleRoot = Split-Path $PSScriptRoot
$puzzleCode = "using System;`nusing System.Collections;`nusing System.Collections.Generic;`nusing System.Linq;`nusing UnityEngine;`n#pragma warning disable 0649`n"
foreach ($puzzleSource in $puzzleSources) {
    $puzzleText = Get-Content -Raw -LiteralPath (Join-Path $puzzleRoot $puzzleSource)
    $puzzleCode += [regex]::Replace($puzzleText, '(?m)^using [^;]+;\r?\n', '') + "`n"
}
$puzzleCode += Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'CapacityRegression.cs.txt')
Add-Type -TypeDefinition $puzzleCode
[CapacityRegression]::EdgeCases()
$puzzleReports = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'puzzle_layout_report.json') | ConvertFrom-Json
foreach ($puzzleReport in $puzzleReports) {
    $puzzleAsset = Get-Content -Raw -LiteralPath (Join-Path $puzzleRoot "Resources/Levels/Level-$($puzzleReport.level)-Layout.asset")
    $puzzleColors = [System.Collections.Generic.List[int]]::new()
    $puzzleBalls = [System.Collections.Generic.List[int[]]]::new()
    foreach ($puzzleCat in [regex]::Matches($puzzleAsset, '(?s)  - _color:.*?(?=  - _color:|  _activeStations:)')) {
        $puzzleColors.Add([int][regex]::Match($puzzleCat.Value, '_color: (\d+)').Groups[1].Value)
        $puzzleHex = [regex]::Match($puzzleCat.Value, '_passengers: ([0-9a-f]+)').Groups[1].Value
        $puzzlePassengers = [System.Collections.Generic.List[int]]::new()
        for ($puzzleOffset=0; $puzzleOffset -lt $puzzleHex.Length; $puzzleOffset+=8) {
            $puzzlePassengers.Add([Convert]::ToInt32($puzzleHex.Substring($puzzleOffset,2),16))
        }
        $puzzleBalls.Add($puzzlePassengers.ToArray())
    }
    $puzzleMask = [regex]::Match($puzzleAsset, '_activeStations: (\w+)').Groups[1].Value
    $puzzleStationCount = [regex]::Matches($puzzleMask, '01').Count
    [CapacityRegression]::Level($puzzleReport.level,$puzzleColors.ToArray(),$puzzleBalls.ToArray(),
        [int[]]$puzzleReport.solution,$puzzleReport.track_capacity,$puzzleStationCount,$true)
    if ($null -ne $puzzleReport.losing_order) {
        [CapacityRegression]::Level($puzzleReport.level,$puzzleColors.ToArray(),$puzzleBalls.ToArray(),
            [int[]]$puzzleReport.losing_order,$puzzleReport.track_capacity,$puzzleStationCount,$false)
    }
}
$puzzleLosingCount = @($puzzleReports | Where-Object { $null -ne $_.losing_order }).Count
Write-Output "PASS: $([CapacityRegression]::Passed) assertions against production sorting/deadlock code; $($puzzleReports.Count) winning and $puzzleLosingCount losing orders, plus edge cases. Rendering/physics are stubbed."
