#!/bin/sh
mkdir -p lib

# library
gmcs -target:library -out:lib/tenderbase.dll src/*.cs src/impl/*.cs

# unit tests. We compile tenderbase code in with unittest code, because
# we need access to internal members, which only works within same assembly
gmcs -reference:bin/nunit.framework.dll -target:library -out:lib/unittests.dll  src/*.cs src/impl/*.cs unittests/*.cs

# tests
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestBackup.exe tests/TestBackup.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestBit.exe tests/TestBit.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestBlob.exe tests/TestBlob.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestComponundIndex.exe tests/TestCompoundIndex.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestConcur.exe tests/TestConcur.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestIndex.exe tests/TestIndex.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestIndex2.exe tests/TestIndex2.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestIndexIterator.exe tests/TestIndexIterator.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestLink.exe tests/TestLink.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestMaxOid.exe tests/TestMaxOid.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestMod.exe tests/TestMod.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestR2.exe tests/TestR2.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestRaw.exe tests/TestRaw.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestReplic.exe tests/TestReplic.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestRtree.exe tests/TestRtree.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestSet.exe tests/TestSet.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestSimple.exe tests/TestSimple.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestThickIndex.exe tests/TestThickIndex.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestTimeSeries.exe tests/TestTimeSeries.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestTtree.exe tests/TestTtree.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestXML.exe tests/TestXML.cs

# examples
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/Guess.exe examples/Guess.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/IpCountry.exe examples/IpCountry.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestSOD.exe examples/TestSOD.cs
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/TestSSD.exe examples/TestSSD.cs

# OO7 benchmark
gmcs -reference:lib/tenderbase.dll -target:exe -out:lib/oo7.exe benchmarks/OO7/*.cs
