#!/bin/bash
mkdir -p lib

GMCS=gmcs2

# library
$GMCS -target:library -out:lib/tenderbase.dll src/*.cs src/impl/*.cs

# unit tests. We compile tenderbase code in with unittest code, because
# we need access to internal members, which only works within same assembly
$GMCS -reference:bin/nunit.framework.dll -target:library -out:lib/unittests.dll  src/*.cs src/impl/*.cs unittests/*.cs

# tests
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestBackup.exe tests/TestBackup.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestBit.exe tests/TestBit.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestBlob.exe tests/TestBlob.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestComponundIndex.exe tests/TestCompoundIndex.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestConcur.exe tests/TestConcur.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestIndex.exe tests/TestIndex.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestIndex2.exe tests/TestIndex2.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestIndexIterator.exe tests/TestIndexIterator.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestLink.exe tests/TestLink.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestMaxOid.exe tests/TestMaxOid.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestMod.exe tests/TestMod.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestR2.exe tests/TestR2.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestRaw.exe tests/TestRaw.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestReplic.exe tests/TestReplic.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestRtree.exe tests/TestRtree.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestSet.exe tests/TestSet.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestSimple.exe tests/TestSimple.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestThickIndex.exe tests/TestThickIndex.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestTimeSeries.exe tests/TestTimeSeries.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestTtree.exe tests/TestTtree.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestXML.exe tests/TestXML.cs

# examples
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/Guess.exe examples/Guess.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/IpCountry.exe examples/IpCountry.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestSOD.exe examples/TestSOD.cs
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/TestSSD.exe examples/TestSSD.cs

# OO7 benchmark
$GMCS -reference:lib/tenderbase.dll -target:exe -out:lib/oo7.exe benchmarks/OO7/*.cs
