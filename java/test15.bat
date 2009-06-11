@rem assumes we've already compiled everything
@echo off
cd build\test15
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndex
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndex
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndex altbtree
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndex altbtree serializable
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndex inmemory
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndex2
java -classpath ..\..\lib\perst15.jar;. TestCompoundIndex
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestCompoundIndex altbtree
java -classpath ..\..\lib\perst15.jar;. TestMod
java -classpath ..\..\lib\perst15.jar;. TestIndexIterator
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestIndexIterator altbtree
java -classpath ..\..\lib\perst15.jar;. TestRtree
java -classpath ..\..\lib\perst15.jar;. TestR2
java -classpath ..\..\lib\perst15.jar;. TestTtree
java -classpath ..\..\lib\perst15.jar;. TestRaw
java -classpath ..\..\lib\perst15.jar;. TestRaw
java -classpath ..\..\lib\perst15.jar;. TestGC
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestGC background
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestGC altbtree background
del *.dbs
java -classpath ..\..\lib\perst15.jar;. TestConcur
java -classpath ..\..\lib\perst15.jar;. TestXML
java -classpath ..\..\lib\perst15.jar;. TestBackup
java -classpath ..\..\lib\perst15.jar;. TestBlob
java -classpath ..\..\lib\perst15.jar;. TestBlob
@rem java -classpath ..\..\lib\perst15.jar;. TestTimeSeries
java -classpath ..\..\lib\perst15.jar;. TestBit
java -classpath ..\..\lib\perst15.jar;. TestThickIndex
java -classpath ..\..\lib\perst15.jar;. TestSet

@rem start TestReplic master
@rem call TestReplic slave
