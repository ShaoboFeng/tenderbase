package org.garret.perst;

/** Class for checking program invariants. Analog of C <code>assert()</code>
 *  macro. The Java compiler doesn't provide information about compiled
 *  file and line number, so the source of assertion failure can be located only
 *  by analyzing the stack trace of the thrown AssertionFailed exception.
 *
 * @see org.garret.perst.AssertionFailed
 */
public class Assert { 
    /** Check specified condition and raise <code>AssertionFailed</code>
     *  exception if it is not true.
     * 
     * @param cond result of checked condition 
     */
    public static final void that(boolean cond) { 
        if (!cond) { 
            throw new AssertionFailed();
        }
    }

    /** Check specified condition and raise <code>AssertionFailed</code>
     *  exception if it is not true. 
     * 
     * @param description string describing checked condition
     * @param cond result of checked condition 
     */
    public static final void that(String description, boolean cond) { 
        if (!cond) { 
            throw new AssertionFailed(description);
        }
    }

    /**
     * Throw AssertionFailed exception.
     */
    public static final void failed() { 
        throw new AssertionFailed();
    }

    /**
     * Throw AssertionFailed exception with given description.
     */
    public static final void failed(String description) { 
        throw new AssertionFailed(description);
    }
}

