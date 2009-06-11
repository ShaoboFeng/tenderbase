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
//UPGRADE_TODO: The type 'TenderBase.IPersistent' could not be found. If it was not included in the conversion, there may be compiler issues. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1262'"
using IPersistent = TenderBase.IPersistent;

public interface OO7_Connection:IPersistent
{
	System.String Type
	{
		set;
		
	}
	long Length
	{
		set;
		
	}
	OO7_AtomicPart From
	{
		set;
		
	}
	OO7_AtomicPart To
	{
		set;
		
	}
	
	
	System.String type();
	
	
	long length();
	
	
	OO7_AtomicPart from();
	
	
	OO7_AtomicPart to();
}