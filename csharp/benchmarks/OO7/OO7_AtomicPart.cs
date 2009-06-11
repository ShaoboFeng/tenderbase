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

public interface OO7_AtomicPart:OO7_DesignObject
{
	long X
	{
		set;
		
	}
	long Y
	{
		set;
		
	}
	long DocId
	{
		set;
		
	}
	OO7_CompositePart PartOf
	{
		set;
		
	}
	
	
	long x();
	
	
	long y();
	
	
	long docId();
	
	
	void  addTo(OO7_Connection x);
	
	
	Link to();
	
	
	void  addFrom(OO7_Connection x);
	
	
	Link from();
	
	
	OO7_CompositePart partOf();
}