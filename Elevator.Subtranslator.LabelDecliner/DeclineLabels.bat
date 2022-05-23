@echo off

set labelDeclinerPath=Elevator.Subtranslator.LabelDecliner.exe
set defInjectedPath=G:\Rimworld translation\RimWorld-ru\Royalty\DefInjected
set defTypes=BodyDef,BodyPartDef,BodyPartGroupDef,HediffDef,PawnCapacityDef,PawnKindDef,ThingDef,SiteCoreDef,SitePartDef,ToolCapacityDef
set outputFile=F:\Rimworld translation\RimWorld-ru\Royalty\WordInfo\Case.txt
set ingnoreFile=\\elevator-nas\projects\Rimworld translation\Info\GendersAndCases\IgnoreCaseRoyalty.txt

%labelDeclinerPath% --defsPath "%defInjectedPath%" --defTypes "%defTypes%" --outputFile "%outputFile%" --ignoreFile "%ingnoreFile%"