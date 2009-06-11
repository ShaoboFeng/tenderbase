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
using TenderBase;

public class OO7_BaseAssemblyImpl:OO7_AssemblyImpl, OO7_BaseAssembly
{
	internal Link theComponentsPriv;
	internal Link theComponentsShar;
	
	protected internal OO7_BaseAssemblyImpl()
	{
	}
	
	public OO7_BaseAssemblyImpl(Storage storage)
	{
		theComponentsPriv = storage.CreateLink();
		theComponentsShar = storage.CreateLink();
	}
	
	
	public virtual void  addComponentsPriv(OO7_CompositePart x)
	{
		theComponentsPriv.Add(x);
		Modify();
	}
	
	
	public virtual Link componetsPriv()
	{
		return theComponentsPriv;
	}
	
	
	public virtual void  addComponentsShar(OO7_CompositePart x)
	{
		theComponentsShar.Add(x);
		Modify();
	}
	
	
	public virtual Link componentsShar()
	{
		return theComponentsShar;
	}
}