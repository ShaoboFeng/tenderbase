@echo off
@mkdir ..\lib
@set SAVE_PATH=%PATH%
call ..\setjdkpath.bat
@echo on

javac -g org\garret\perst\*.java org\garret\perst\impl\*.java org\garret\perst\impl\sun14\*.java
jar cf ..\lib\perst.jar org\garret\perst\*.class org\garret\perst\impl\*.class org\garret\perst\impl\sun14\*.class

@echo off
@set PATH=%SAVE_PATH%
@echo on


