#!/bin/sh
cd build/test15
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndex15
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndex
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndex altbtree
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndex altbtree serializable
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndex inmemory
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndex2
java -classpath ../../lib/perst15.jar:. TestCompoundIndex
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestCompoundIndex altbtree
java -classpath ../../lib/perst15.jar:. TestMod
java -classpath ../../lib/perst15.jar:. TestIndexIterator
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestIndexIterator altbtree
java -classpath ../../lib/perst15.jar:. TestRtree
java -classpath ../../lib/perst15.jar:. TestR2
java -classpath ../../lib/perst15.jar:. TestTtree
java -classpath ../../lib/perst15.jar:. TestRaw
java -classpath ../../lib/perst15.jar:. TestRaw
java -classpath ../../lib/perst15.jar:. TestGC
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestGC background
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestGC altbtree background
rm *.dbs
java -classpath ../../lib/perst15.jar:. TestConcur
java -classpath ../../lib/perst15.jar:. TestXML
java -classpath ../../lib/perst15.jar:. TestBackup
java -classpath ../../lib/perst15.jar:. TestBlob
java -classpath ../../lib/perst15.jar:. TestBlob
#java -classpath ../../lib/perst15.jar:. TestTimeSeries
java -classpath ../../lib/perst15.jar:. TestBit
java -classpath ../../lib/perst15.jar:. TestThickIndex
java -classpath ../../lib/perst15.jar:. TestSet

#start TestReplic master
#call TestReplic slave
