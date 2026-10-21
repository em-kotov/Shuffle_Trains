$ErrorActionPreference = 'Stop'
$hintRoot = Split-Path $PSScriptRoot
$hintSources = @(
    'Scripts/SorterRegistrator/CatColor.cs', 'Scripts/SorterRegistrator/SeatTracker.cs',
    'Scripts/SorterRegistrator/SorterRegistrator.cs', 'Scripts/Car/SorterCar/PassengerTracker.cs',
    'Scripts/Car/SorterCar/StationOperator.cs', 'Scripts/Car/SorterCar/SorterCar.cs',
    'Scripts/Station/Station.cs', 'Scripts/Passenger/Passenger.cs', 'Scripts/LevelManagers/CatHintPlanner.cs',
    'Tools/CapacityRegression.cs.txt', 'Tools/HintRegression.cs.txt'
)
$hintCode = "using System;`nusing System.Collections;`nusing System.Collections.Generic;`nusing System.Linq;`nusing System.Text;`nusing UnityEngine;`n#pragma warning disable 0649`n"
foreach ($hintSource in $hintSources) {
    $hintText = Get-Content -Raw -LiteralPath (Join-Path $hintRoot $hintSource)
    $hintCode += [regex]::Replace($hintText, '(?m)^using [^;]+;\r?\n', '') + "`n"
}
Add-Type -TypeDefinition $hintCode
[CapacityRegression]::HintEdges()
$hintReports = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'puzzle_layout_report.json') | ConvertFrom-Json
foreach ($hintReport in $hintReports) {
    $hintAsset = Get-Content -Raw -LiteralPath (Join-Path $hintRoot "Resources/Levels/Level-$($hintReport.level)-Layout.asset")
    $hintColors = [System.Collections.Generic.List[int]]::new()
    $hintBalls = [System.Collections.Generic.List[int[]]]::new()
    $hintPoses = [System.Collections.Generic.List[int[]]]::new()
    foreach ($hintCat in [regex]::Matches($hintAsset, '(?s)  - _color:.*?(?=  - _color:|  _activeStations:)')) {
        $hintColors.Add([int][regex]::Match($hintCat.Value, '_color: (\d+)').Groups[1].Value)
        $hintHex = [regex]::Match($hintCat.Value, '_passengers: ([0-9a-f]+)').Groups[1].Value
        $hintPassengers = [System.Collections.Generic.List[int]]::new()
        for ($hintOffset=0; $hintOffset -lt $hintHex.Length; $hintOffset+=8) {
            $hintPassengers.Add([Convert]::ToInt32($hintHex.Substring($hintOffset,2),16))
        }
        $hintBalls.Add($hintPassengers.ToArray())
        $hintCell = [regex]::Match($hintCat.Value, '_cell: \{x: (-?\d+), y: (-?\d+)\}')
        $hintDirection = 2 * [int][regex]::Match($hintCat.Value, '_orientation: (\d)').Groups[1].Value
        if ([float][regex]::Match($hintCat.Value, '_signDirection: (-?[\d.]+)').Groups[1].Value -lt 0) { $hintDirection++ }
        $hintPoses.Add([int[]]@([int]$hintCell.Groups[1].Value,[int]$hintCell.Groups[2].Value,$hintDirection))
    }
    $hintMask = [regex]::Match($hintAsset, '_activeStations: (\w+)').Groups[1].Value
    $hintStationCount = [regex]::Matches($hintMask, '01').Count
    [CapacityRegression]::HintLevel($hintReport.level,$hintColors.ToArray(),$hintBalls.ToArray(),
        $hintPoses.ToArray(),$hintReport.track_capacity,$hintStationCount)
}
Write-Output "PASS: $([CapacityRegression]::Passed) hint assertions. All $($hintReports.Count) levels completed by following hints through production sorting logic. Rendering/physics are stubbed."
