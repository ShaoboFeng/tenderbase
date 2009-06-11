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

public interface OO7_Module:OO7_DesignObject
{
	OO7_Manual Manual
	{
		set;
	}

	OO7_ComplexAssembly DesignRoot
	{
		set;
	}
	
	OO7_Manual manual();
	void  addAssembly(OO7_Assembly x);
	Link assembly();
	
	
	OO7_ComplexAssembly designRoot();
	
	
	OO7_AtomicPart getAtomicPartByName(System.String name);
	
	
	void  addAtomicPart(System.String name, OO7_AtomicPart part);
}