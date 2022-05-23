@echo off

set unityExPath=UnityEX.exe
set backstoryUpdaterPath=Subtranslator\Elevator.Subtranslator.BackstoryUpdater.exe
set resourceHistoryRootPath=ResourcesHistory
set rimworldGamePath=G:\Steam\steamapps\common\RimWorld
set rimworldLocalizationBackstoriesPath=F:\Rimworld translation\RimWorld-ru\Core\Backstories
set /p version_contents=<%rimworldGamePath%\version.txt
set version=%version_contents:~4,4%
set oldVersion=%1

echo Detected game version %version%

%unityExPath% export %rimworldGamePath%\RimWorldWin64_Data\resources.assets  -t 49 -p "%resourceHistoryRootPath%\resources-%version%"

%backstoryUpdaterPath% -p "%resourceHistoryRootPath%\resources-%oldVersion%\Unity_Assets_Files\resources" -r "%resourceHistoryRootPath%\resources-%version%\Unity_Assets_Files\resources" -t "%rimworldLocalizationBackstoriesPath%\Backstories.xml" -o "%rimworldLocalizationBackstoriesPath%\Backstories.xml"