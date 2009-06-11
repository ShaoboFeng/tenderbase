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
using Link = TenderBase.Link;

public interface OO7_CompositePart:OO7_DesignObject
{
	OO7_Document Documentation
	{
		set;	
	}
	OO7_AtomicPart RootPart
	{
		set;		
	}

	OO7_Document documentation();
	void  addUsedInPriv(OO7_BaseAssembly x);
	Link usedInPriv();	
	void  addUsedInShar(OO7_BaseAssembly x);	
	Link usedInShar();
	void  addPart(OO7_AtomicPart x);
	Link parts();
	OO7_AtomicPart rootPart();
}