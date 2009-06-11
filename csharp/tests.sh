#!/bin/sh

mkdir lib

gmcs -target:library -out:lib/nuperst.dll src/*.cs src/impl/*.cs

gmcs reference:lib/nuperst.dll -target:exe -out:lib/TestSimple.exe test/TestSimple.cs
