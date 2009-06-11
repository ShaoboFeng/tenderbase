// You can redistribute this software and/or modify it under the terms of
// the Ozone Core License version 1 published by ozone-db.org.
//
// The original code and portions created by Thorsten Fiebig are
// Copyright (C) 2000-@year@ by Thorsten Fiebig. All rights reserved.
// Code portions created by SMB are
// Copyright (C) 1997-@year@ by SMB GmbH. All rights reserved.
//
// $Id$
using System;
//UPGRADE_TODO: The package 'TenderBase' could not be found. If it was not included in the conversion, there may be compiler issues. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1262'"
using TenderBase;

public class OO7_ModuleImpl:OO7_DesignObjectImpl, OO7_Module
{
	virtual public OO7_Manual Manual
	{
		set
		{
			theManual = value;
			Modify();
		}
		
	}
	virtual public OO7_ComplexAssembly DesignRoot
	{
		set
		{
			theDesignRoot = value;
			Modify();
		}
		
	}
	internal OO7_Manual theManual;
	internal Link theAssembly;
	internal Index theComponents;
	internal OO7_ComplexAssembly theDesignRoot;
	
	
	private OO7_ModuleImpl()
	{
	}
	
	public OO7_ModuleImpl(Storage storage)
	{
		theAssembly = storage.CreateLink();
		theComponents = storage.CreateIndex(typeof(System.String), true);
	}
	
	
	public virtual OO7_Manual manual()
	{
		return theManual;
	}
	
	
	public virtual void  addAssembly(OO7_Assembly x)
	{
		theAssembly.Add(x);
		Modify();
	}
	
	
	public virtual Link assembly()
	{
		return theAssembly;
	}
	
	
	public virtual OO7_ComplexAssembly designRoot()
	{
		return theDesignRoot;
	}
	
	public virtual OO7_AtomicPart getAtomicPartByName(System.String name)
	{
		return (OO7_AtomicPart) theComponents.Get(name);
	}
	
	
	public virtual void  addAtomicPart(System.String name, OO7_AtomicPart part)
	{
		theComponents.Put(name, part);
	}
}