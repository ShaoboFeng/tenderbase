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

public class OO7_DocumentImpl:Persistent, OO7_Document
{
	virtual public System.String Title
	{
		set
		{
			theTitle = value;
			Modify();
		}
		
	}
	virtual public long Id
	{
		set
		{
			theId = value;
			Modify();
		}
		
	}
	virtual public System.String Text
	{
		set
		{
			theText = value;
			Modify();
		}
		
	}
	internal System.String theTitle;
	internal long theId;
	internal System.String theText;
	
	
	public OO7_DocumentImpl()
	{
	}
	
	
	public virtual System.String title()
	{
		return theTitle;
	}
	
	
	public virtual long id()
	{
		return theId;
	}
	
	
	public virtual System.String text()
	{
		return theText;
	}
}