#!/bin/sh
cd build/testpre15
rm *.dbs
java -classpath ../../lib/perst.jar:. TestIndex
rm *.dbs
java -classpath ../../lib/perst.jar:. TestIndex altbtree
rm *.dbs
java -classpath ../../lib/perst.jar:. TestIndex altbtree serializable
rm *.dbs
java -classpath ../../lib/perst.jar:. TestIndex inmemory
rm *.dbs
java -classpath ../../lib/perst.jar:. TestIndex2
java -classpath ../../lib/perst.jar:. TestCompoundIndex
rm *.dbs
java -classpath ../../lib/perst.jar:. TestCompoundIndex altbtree
java -classpath ../../lib/perst.jar:. TestMod
java -classpath ../../lib/perst.jar:. TestIndexIterator
rm *.dbs
java -classpath ../../lib/perst.jar:. TestIndexIterator altbtree
java -classpath ../../lib/perst.jar:. TestRtree
java -classpath ../../lib/perst.jar:. TestR2
java -classpath ../../lib/perst.jar:. TestTtree
java -classpath ../../lib/perst.jar:. TestRaw
java -classpath ../../lib/perst.jar:. TestRaw
java -classpath ../../lib/perst.jar:. TestGC
rm *.dbs
java -classpath ../../lib/perst.jar:. TestGC background
rm *.dbs
java -classpath ../../lib/perst.jar:. TestGC altbtree background
rm *.dbs
java -classpath ../../lib/perst.jar:. TestConcur
java -classpath ../../lib/perst.jar:. TestXML
java -classpath ../../lib/perst.jar:. TestBackup
java -classpath ../../lib/perst.jar:. TestBlob
java -classpath ../../lib/perst.jar:. TestBlob
java -classpath ../../lib/perst.jar:. TestTimeSeries
java -classpath ../../lib/perst.jar:. TestBit
java -classpath ../../lib/perst.jar:. TestThickIndex
java -classpath ../../lib/perst.jar:. TestSet
#start TestReplic master
#call TestReplic slave
