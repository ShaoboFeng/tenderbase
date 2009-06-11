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

csc /define:OMIT_TIME_SERIES /target:library /debug+ /out:lib\tenderbase.dll src\*.cs src\impl\*.cs

csc /define:OMIT_TIME_SERIES /reference:lib\tenderbase.dll /debug+ /target:exe /out:lib\TestSimple.exe test\TestSimple.cs

csc /define:OMIT_TIME_SERIES /reference:lib\tenderbase.dll /debug+ /target:exe /out:lib\TestIndex.exe tst\TestIndex.cs
