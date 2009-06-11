using System;
// You can redistribute this software and/or modify it under the terms of
// the Ozone Core License version 1 published by ozone-db.org.
//
// The original code and portions created by Thorsten Fiebig are
// Copyright (C) 2000-@year@ by Thorsten Fiebig. All rights reserved.
// Code portions created by SMB are
// Copyright (C) 1997-@year@ by SMB GmbH. All rights reserved.
//
// $Id$


public class OO7_AssemblyImpl:OO7_DesignObjectImpl, OO7_Assembly
{
	virtual public OO7_ComplexAssembly SuperAssembly
	{
		set
		{
			theSuperAssembly = value;
		}
		
	}
	virtual public OO7_Module Module
	{
		set
		{
			theModule = value;
			Modify();
		}
		
	}
	internal OO7_ComplexAssembly theSuperAssembly;
	internal OO7_Module theModule;
	
	
	public virtual OO7_ComplexAssembly superAssembly()
	{
		return theSuperAssembly;
	}
	
	
	public virtual OO7_Module module()
	{
		return theModule;
	}
}