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
csc /define:OMIT_TIME_SERIES /target:library /out:lib\tenderbase.dll src\*.cs src\impl\*.cs
