@echo off  
SETLOCAL ENABLEDELAYEDEXPANSION
set files=%*
set filesString=
for %%s in (%files%) DO (
	call :concat %%s
)
START /B /WAIT obj2fsnp.exe %filesString%
for %%s in (%filesString%) DO (
	set str=%%s
	set str=!str:%"=!
	set str2=%%s
	set str2=!str2:%"=!
	set str=!str!.o2f
	set str2=!str2!.2013
	START /B /WAIT AssetCc2_fixed.exe "--strip" "--rules=4101" "!str!" "!str2!"
)
del *.o2f
set filelist=
set filelist2=
::for %%s in %* DO (set filelist=%filelist%%%s;)
for %%s in (%filesString%) DO call :concat2013 %%s
START /B /WAIT hknp2fsnp.exe %filelist%
del /q *.2013
for %%s in (%filesString%) DO (
	set str=%%s
	set str=!str:%"=!
	set str2=%%s
	set str2=!str2:%"=!
	set str=!str!.2013.hkx
	set str2=!str2!.hkx
	if exist "%%~ns.hkx" (
		del "%%~ns.hkx"
	)
	ren "!str!" "%%~ns.hkx"
)
EXIT /B 0

:concatSimple 
set str=%str%%1
EXIT /B 0

:concat
set filesString=%filesString%%1;
EXIT /B 0

:concat2013
set filelist=%filelist%%1.2013;
EXIT /B 0