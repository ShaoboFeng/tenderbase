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

public interface OO7_Manual:IPersistent
{
	System.String Title
	{
		set;
		
	}
	long Id
	{
		set;
		
	}
	System.String Text
	{
		set;
		
	}
	OO7_Module Module
	{
		set;
		
	}
	
	
	System.String title();
	
	
	long id();
	
	
	System.String text();
	
	
	OO7_Module module();
}