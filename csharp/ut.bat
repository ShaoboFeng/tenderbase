@rem @call "%PROGRAMFILES%\Microsoft Visual Studio 9.0\Common7\Tools\vsvars32.bat"
@call "%VS90COMNTOOLS%\vsvars32.bat"

@rem Defines for omitting parts of the code:
@rem Those must be provided because their code doesn't work yet
@rem OMIT_TIME_SERIES
@rem
@rem OMIT_MULTIFILE
@rem OMIT_XML
@rem OMIT_REPLICATION
@rem OMIT_PATRICIA_TRIE
@rem OMIT_RTREE
@rem OMIT_RTREER2

mkdir lib
@rem I would rather /reference:tenderbase.dll, but due to extensive use of 'internal' keyword,
@rem I have to recompile so that unit tests are part of the same assembly
csc /define:OMIT_TIME_SERIES /target:library /out:lib\tenderbase-tests.dll /reference:bin\nunit.framework.dll src\*.cs src\impl\*.cs unittests\*.cs

gacutil /i bin\nunit.framework.dll /f

bin\nunit-console lib\tenderbase-tests.dll
