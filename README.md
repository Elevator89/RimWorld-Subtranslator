# RimWorld-Subtranslator
A simple command line tool for helping with translation of RimWorld game. The tool reads target and 'etalon' DefInjected localizations, compares them, fills new injections with values from Def folder, and provides a report file. 

The report file consists of:

* `Items to translate` section. This section lists missing (not translated) injections in target localization. These injections have non-translated English strings from original Def folder.

* `Items to delete` section. Here go injections that don't exits in the etalon localization. They can be safely deleted from localization.

* `Items to move` section. These lines show which injections were probably renamed or moved from their original location into another one.


## Usage
Example:

```
-d "G:\Rimworld translation\Translation\English\Defs" -i "G:\Rimworld translation\Translation\Russian\DefInjected" -e "G:\Rimworld translation\Translation\German\DefInjected" -r "G:\Rimworld translation\Translation\InjectionsReport.txt"
```


`-d, --defs` - path to Def folder (required);

`-i, --injections` - path to target localization 'DefInjected' folder (required);

`-e, --etalon` - path to etalon localization 'DefInjected' folder (required);

`-a, --append` - append new translation lines to current localization or not (not requered, default: false);

`-r, --report` - report file output path (required);

`--help` - display the help screen.
