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


public class OO7_CompositePartImpl:OO7_DesignObjectImpl, OO7_CompositePart
{
	virtual public OO7_Document Documentation
	{
		set
		{
			theDocumentation = value;
			Modify();
		}
		
	}
	virtual public OO7_AtomicPart RootPart
	{
		set
		{
			theRootPart = value;
			Modify();
		}
		
	}
	internal OO7_Document theDocumentation;
	internal Link theUsedInPriv;
	internal Link theUsedInShar;
	internal Link theParts;
	internal OO7_AtomicPart theRootPart;
	
	
	private OO7_CompositePartImpl()
	{
	}
	
	public OO7_CompositePartImpl(Storage storage)
	{
		theUsedInPriv = storage.CreateLink();
		theUsedInShar = storage.CreateLink();
		theParts = storage.CreateLink();
	}
	
	
	public virtual OO7_Document documentation()
	{
		return theDocumentation;
	}
	
	
	public virtual void  addUsedInPriv(OO7_BaseAssembly x)
	{
		theUsedInPriv.Add(x);
		Modify();
	}
	
	
	public virtual Link usedInPriv()
	{
		return theUsedInPriv;
	}
	
	
	public virtual void  addUsedInShar(OO7_BaseAssembly x)
	{
		theUsedInShar.Add(x);
		Modify();
	}
	
	
	public virtual Link usedInShar()
	{
		return theUsedInShar;
	}
	
	
	public virtual void  addPart(OO7_AtomicPart x)
	{
		theParts.Add(x);
		Modify();
	}
	
	
	public virtual Link parts()
	{
		return theParts;
	}
	
	
	public virtual OO7_AtomicPart rootPart()
	{
		return theRootPart;
	}
}