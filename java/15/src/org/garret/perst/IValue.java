package org.garret.perst;

/**
 * Interface of objects stored as value. Value objects are stored inside the persistent object to which they belong
 * and not as separate instances. Value field can not contain null values. When value object is changed, 
 * programmer should call <code>store</code> method of persistent class containing this value. 
 */
public interface IValue { 
}
