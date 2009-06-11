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

public class OO7_DesignObjectImpl:Persistent, OO7_DesignObject
{
	virtual public long Id
	{
		set
		{
			theId = value;
			Modify();
		}
		
	}
	virtual public System.String Type
	{
		set
		{
			theType = value;
			Modify();
		}
		
	}
	virtual public long BuildDate
	{
		set
		{
			theBuildDate = value;
			Modify();
		}
		
	}
	internal long theId;
	internal System.String theType;
	internal long theBuildDate;
	
	
	protected internal OO7_DesignObjectImpl()
	{
	}
	
	public OO7_DesignObjectImpl(System.String type)
	{
		theType = type;
	}
	
	
	public virtual long id()
	{
		return theId;
	}
	
	
	public virtual System.String type()
	{
		return theType;
	}
	
	
	public virtual long buildDate()
	{
		return theBuildDate;
	}
}