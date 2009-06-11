#!/bin/sh

rm -rf build lib
mkdir -p build/pre15/org/garret/perst/impl
mkdir -p build/15/org/garret/perst/impl
mkdir -p build/testpre15
mkdir -p build/test15
mkdir -p lib

cd src
javac -source 1.4 -g -d ../build/pre15 org/garret/perst/*.java org/garret/perst/impl/*.java org/garret/perst/impl/sun14/*.java
cd ../build/pre15
jar cf ../../lib/perst.jar org/garret/perst/*.class org/garret/perst/impl/*.class org/garret/perst/impl/sun14/*.class

cd ../../src15
javac -source 1.5 -g -d ../build/15 org/garret/perst/*.java org/garret/perst/impl/*.java org/garret/perst/impl/sun14/*.java
cd ../build/15
jar cf ../../lib/perst15.jar org/garret/perst/*.class org/garret/perst/impl/*.class org/garret/perst/impl/sun14/*.class

# compile common and pre15-specific code in pre-15 mode
cd ../../tst
javac -source 1.4 -g -d ../build/testpre15 -classpath ../lib/perst.jar *.java

cd pre15
javac -source 1.4 -g -d ../../build/testpre15 -classpath ../../lib/perst.jar *.java

# compile common and 15-specific code in 15 mode
cd ..
javac -source 1.5 -g -d ../build/test15 -classpath ../lib/perst15.jar *.java

cd 15
javac -source 1.5 -g -d ../../build/test15 -classpath ../../lib/perst15.jar TestIndex15.java

