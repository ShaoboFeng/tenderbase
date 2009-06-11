package org.garret.perst;

/**
 * Exception raised by <code>Assert</code> class on failed assertion.
 */
public class AssertionFailed extends Error {
    AssertionFailed() { 
        super("Assertion failed");
    }

    AssertionFailed(String description) { 
        super("Assertion '" + description + "' failed");
    }
}
