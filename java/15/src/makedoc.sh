#!/bin/sh
mkdir -p ../doc15
javadoc -source 1.5 -d ../doc15 -nodeprecated -nosince -public org/garret/perst/*.java
