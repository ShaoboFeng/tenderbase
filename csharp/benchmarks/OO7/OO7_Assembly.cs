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


public interface OO7_Assembly:OO7_DesignObject
{
	OO7_ComplexAssembly SuperAssembly
	{
		set;
		
	}
	OO7_Module Module
	{
		set;
		
	}
	
	
	OO7_ComplexAssembly superAssembly();
	
	
	OO7_Module module();
}