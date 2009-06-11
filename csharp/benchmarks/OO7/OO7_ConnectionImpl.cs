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
//UPGRADE_TODO: The type 'TenderBase.Persistent' could not be found. If it was not included in the conversion, there may be compiler issues. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1262'"
using Persistent = TenderBase.Persistent;


public class OO7_ConnectionImpl:Persistent, OO7_Connection
{
	virtual public System.String Type
	{
		set
		{
			theType = value;
			Modify();
		}
		
	}
	virtual public long Length
	{
		set
		{
			theLength = value;
			Modify();
		}
		
	}
	virtual public OO7_AtomicPart From
	{
		set
		{
			theFrom = value;
			Modify();
		}
		
	}
	virtual public OO7_AtomicPart To
	{
		set
		{
			theTo = value;
			Modify();
		}
		
	}
	internal System.String theType;
	internal long theLength;
	internal OO7_AtomicPart theFrom;
	internal OO7_AtomicPart theTo;
	
	
	private OO7_ConnectionImpl()
	{
	}
	
	public OO7_ConnectionImpl(System.String type)
	{
		theType = type;
	}
	
	
	public virtual System.String type()
	{
		return theType;
	}
	
	
	public virtual long length()
	{
		return theLength;
	}
	
	
	public virtual OO7_AtomicPart from()
	{
		return theFrom;
	}
	
	
	public virtual OO7_AtomicPart to()
	{
		return theTo;
	}
}