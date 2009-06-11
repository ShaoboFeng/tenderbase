#!/usr/bin/env python
import os.path
import sys
import re

SCRIPT_DIR = os.path.realpath(os.path.dirname(__file__))

FILES_TO_CHECK = (
    #(".", "\.txt$"),
    ("csharp", "src", "\.cs$"),
    ("csharp", "src",  "impl", "\.cs$"),
    ("csharp", "unittests", "\.cs$"),
    ("csharp", "tests", "\.cs$"),
    ("csharp", "examples", "\.cs$"),
    ("java", "pre15", "src", "org", "garret", "perst", "\.java$"),
    ("java", "pre15", "src", "org", "garret", "perst", "impl", "\.java$"),
    ("java", "pre15", "tst", "\.java$"),
    ("java", "pre15", "examples", "\.java$"),
    ("java", "pre15", "OO7", "\.java$"),
    ("java", "15", "src", "org", "garret", "perst", "\.java$"),
    ("java", "15", "src", "org", "garret", "perst", "impl", "\.java$"),
)

TRAILING_WHITESPACE_RE = "[ \t]+$"

def matches(pattern, name): return re.search(pattern, name) != None

def test_file_for_trailing_whitespace(f):
    lineno = 1
    error_count = 0
    fo = open(f, "r")
    for l in fo.readlines():
        match = re.search(TRAILING_WHITESPACE_RE, l)
        if match != None:
            if l[-1] == "\n": l = l[:-1]
            fo.close()
            if error_count == 0:
                print(f)
            print("  line %d: '%s'" % (lineno, l))
            error_count += 1
        lineno += 1
    fo.close()

def main():
    #print("SCRIPT_DIR: %s" % SCRIPT_DIR)
    for dir_info in FILES_TO_CHECK:
        dir = SCRIPT_DIR
        path = os.path.join(dir, *dir_info[:-1])
        pattern = dir_info[-1]
        #print("checking '%s' for '%s'" % (path, pattern))
        files = [os.path.realpath(os.path.join(path, f)) for f in os.listdir(path)]
        files = [f for f in files if os.path.isfile(f) and matches(pattern, f)]
        for f in files:
            test_file_for_trailing_whitespace(f)

if __name__ == "__main__":
    main()