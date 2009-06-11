@rem assumes we've already compiled everything
@echo off

cd build\testpre15
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestIndex
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestIndex altbtree
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestIndex altbtree serializable
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestIndex inmemory
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestIndex2
java -classpath ..\..\lib\perst.jar;. TestCompoundIndex
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestCompoundIndex altbtree
java -classpath ..\..\lib\perst.jar;. TestMod
java -classpath ..\..\lib\perst.jar;. TestIndexIterator
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestIndexIterator altbtree
java -classpath ..\..\lib\perst.jar;. TestRtree
java -classpath ..\..\lib\perst.jar;. TestR2
java -classpath ..\..\lib\perst.jar;. TestTtree
java -classpath ..\..\lib\perst.jar;. TestRaw
java -classpath ..\..\lib\perst.jar;. TestRaw
java -classpath ..\..\lib\perst.jar;. TestGC
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestGC background
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestGC altbtree background
del *.dbs
java -classpath ..\..\lib\perst.jar;. TestConcur
java -classpath ..\..\lib\perst.jar;. TestXML
java -classpath ..\..\lib\perst.jar;. TestBackup
java -classpath ..\..\lib\perst.jar;. TestBlob
java -classpath ..\..\lib\perst.jar;. TestBlob
java -classpath ..\..\lib\perst.jar;. TestTimeSeries
java -classpath ..\..\lib\perst.jar;. TestBit
java -classpath ..\..\lib\perst.jar;. TestThickIndex
java -classpath ..\..\lib\perst.jar;. TestSet

@rem start TestReplic master
@rem call TestReplic slave
