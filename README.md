# BackstoryUpdater

This tool analyzes backstories from resources of the game (you need to extract them before), compares them with ones in Backstories.xml for your language, and updates your Backstories.xml file:
* adds English comments
* replaces new lines with '\n'
* updates backstory IDs
* divides backstories into groups and places them in the specific order
* preserves the order of stories in each group

The order is following;
1. translated regular backstories
2. not translated regular backstories
3. translated "solid" backstories
4. not translated "solid" backstories
5. translated but unused backstories

This tool will help you:
* to see which lines were changed after a game update
* to check if your translations correspond to the original texts
* to optimize your work with backstories in future

The tool works in two modes:

**Format mode** works with one resourses folder as described above. This mode doesn't change IDs of backstories. This is helpful to prepare your Backstories for future migrations with this tool.

**Migrate mode** works with two resourses folder (e.g. for previous and current versions of the game). The mode maps old and new backstories from resources and change IDs of your backstories accordingly. It is useful to migrate your backstories to the newer version of the game.

## Use case

You haven't been updating your backstories for a long time. The game was updated several times, and a lot of already translated backsoties became invalid. To cope with that you need to:

1. Download UnityEX tool: https://yadi.sk/d/m3vFWoQ3j62Cr, extract the .exe file into some folder
2. In the same folder create a simple "get-resources.bat" file with contents like that:
```
@echo off

set rimworldGamePath=E:\Steam\steamapps\common\RimWorld

set /p version_contents=<%rimworldGamePath%\version.txt
set version=%version_contents:~4,4%
set resources_folder="resources-%version%"

UnityEX.exe export %rimworldGamePath%\RimWorldWin64_Data\resources.assets  -t 49 -p %resources_folder%
```
Set rimworldGamePath variable to the value actual for your case.
This script will extract Rimworld text resources to the local folder named "resources-<game version>".

3. Use Steam to rollback Rimworld to the version, for which your backstories are currently translated for.
4. Run get-resources.bat, it will extract text resources for old version of the game to some folder, e.g. "resources-2408"
5. **Format** your backstories:
`Elevator.Subtranslator.BackstoryUpdater.exe -r <path to resources-2408 folder>\Unity_Assets_Files\resources -t <path to your Backstories.xml file> -o <path to output backstories, let it be the same Backstories.xml>`.
6. Check the diff for Backstories.xml. See English texts in comments, they are actual for version 2408. Commit the file for historical purposes.
7. Use Steam to update Rimworld to the latest version.
8. Run get-resources.bat, it will extract text resources for new version of the game to some folder, e.g. "resources-2624"
9. **Migrate** your backstories:
`Elevator.Subtranslator.BackstoryUpdater.exe -p <path to resources-2408 folder>\Unity_Assets_Files\resources -r <path to resources-2624 folder>\Unity_Assets_Files\resources -t <path to your Backstories.xml file> -o <path to output backstories, let it be the same Backstories.xml>` (note the "-p" argument).
10. Check the diff for Backstories.xml. See some English texts, which have been updated for version 2624. See sections of new lines with new backstories (according to the groupng). See unused translated stories in the end of file.
11. Update translations according to the diff.

Congratulations! You are now able to use this tool!
