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


public class OO7_AtomicPartImpl:OO7_DesignObjectImpl, OO7_AtomicPart
{
	virtual public long X
	{
		set
		{
			theX = value;
			Modify();
		}
	}

	virtual public long Y
	{
		set
		{
			theY = value;
			Modify();
		}
	}

	virtual public long DocId
	{
		set
		{
			theDocId = value;
			Modify();
		}
	}

	virtual public OO7_CompositePart PartOf
	{
		set
		{
			thePartOf = value;
			Modify();
		}
	}

	internal long theX;
	internal long theY;
	internal long theDocId;
	internal Link theToConnections;
	internal Link theFromConnections;
	internal OO7_CompositePart thePartOf;

	private OO7_AtomicPartImpl()
	{
	}
	
	public OO7_AtomicPartImpl(Storage storage)
	{
		theToConnections = storage.CreateLink();
		theFromConnections = storage.CreateLink();
	}
	
	
	public virtual long x()
	{
		return theX;
	}
	
	
	public virtual long y()
	{
		return theY;
	}
	
	
	public virtual long docId()
	{
		return theDocId;
	}

	public virtual void  addTo(OO7_Connection x)
	{
		theToConnections.Add(x);
		Modify();
	}
	
	public virtual Link to()
	{
		return theToConnections;
	}

	public virtual void  addFrom(OO7_Connection x)
	{
		theFromConnections.Add(x);
		Modify();
	}
	
	
	public virtual Link from()
	{
		return theFromConnections;
	}
	
	
	public virtual OO7_CompositePart partOf()
	{
		return thePartOf;
	}
}