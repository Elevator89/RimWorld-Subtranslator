set rimworldGamePath=E:\Steam\steamapps\common\RimWorld
set rimworldLocalizationPath=G:\Rimworld translation\_Translation\Russian
set rimworldPersistentPath=C:\Users\%username%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios
set backstoriesVault=G:\Rimworld translation\Backstories\StoryMap
set /p version_contents=<%rimworldGamePath%\version.txt
set version=%version_contents:~4,4%
set oldVersion=%1

md temp
UnityEX.exe export %rimworldGamePath%\RimWorldWin64_Data\resources.assets  -t 49 -p "temp"
move /Y "temp\Unity_Assets_Files\resources\rimworld_creations.xml" "%backstoriesVault%\rimworld_creations_%version%.xml"
move /Y "%rimworldLocalizationPath%\Backstories\Backstories.xml" "%rimworldLocalizationPath%\Backstories\Backstories_temp.xml"

del /F /Q "%rimworldPersistentPath%\DevOutput\Fresh_Backstories.xml"
set /p DUMMY=Run RimWorld, generate fresh backstories and hit ENTER to continue...
move /Y "%rimworldPersistentPath%\DevOutput\Fresh_Backstories.xml" "%backstoriesVault%\Fresh_Backstories_%version%.xml"

Subtranslator\Elevator.Subtranslator.BackstoryReindexer.exe -l "%backstoriesVault%\Fresh_Backstories_%oldVersion%.xml" -n "%backstoriesVault%\Fresh_Backstories_%version%.xml" -t "%rimworldLocalizationPath%\Backstories\Backstories_temp.xml" -o "%rimworldLocalizationPath%\Backstories\Backstories_updated.xml"

Subtranslator\Elevator.Subtranslator.BackstorySolidAnalyzer.exe -c "%backstoriesVault%\rimworld_creations_%version%.xml" -b "%backstoriesVault%\Fresh_Backstories_%version%.xml" -t "%rimworldLocalizationPath%\Backstories\Backstories_updated.xml" -o "%rimworldLocalizationPath%\Backstories\Backstories.xml"
