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
using TenderBase;

public class BenchmarkImpl : Persistent, Benchmark
{
	virtual protected internal long AtomicPartOid
	{
		get
		{
			return theOid++;
		}
	}

    // database parameters
	private const int fTest1Conn = 0;
	private const int fTest3Conn = 1;
	private const int fTiny = 2;
	private const int fSmall = 3;
	
	private static readonly int[] fNumAtomicPerComp = new int[]{20, 20, 20, 20};
	private static readonly int[] fConnPerAtomic = new int[]{1, 3, 3, 3};
	private static readonly int[] fDocumentSize = new int[]{20, 20, 20, 2000};
	private static readonly int[] fManualSize = new int[]{1000, 1000, 1000, 100000};
	private static readonly int[] fNumCompPerModule = new int[]{5, 5, 50, 500};
	private static readonly int[] fNumAssmPerAssm = new int[]{3, 3, 3, 3};
	private static readonly int[] fNumAssmLevels = new int[]{3, 3, 7, 7};
	private static readonly int[] fNumCompPerAssm = new int[]{3, 3, 3, 3};
	private static readonly int[] fNumModules = new int[]{1, 1, 1, 1};
	
	internal const bool verbose = false;
	
	internal static System.Random theRandom = null;
	
	internal int theScale = 0;
	
	internal long theOid = 0;
	
	internal OO7_Module theModule = null;
	
	private const int pagePoolSize = 32 * 1024 * 1024;
	
	[STAThread]
	public static void  Main(System.String[] args)
	{
		if (args.Length == 0)
		{
			printUsage();
			System.Environment.Exit(1);
		}
		else
		{
			if (args.Length == 1 && (System.Object) args[0] == (System.Object) "query")
			{
				printUsage();
				System.Environment.Exit(1);
			}
		}
		
		Storage db = StorageFactory.Instance.CreateStorage();
		
		db.Open("007.dbs", pagePoolSize);
		
		long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
		
		if (args[0].Equals("query"))
		{
			if (args[1].Equals("traversal"))
			{
				Benchmark anBenchmark = (Benchmark) db.GetRoot();
				anBenchmark.traversalQuery();
			}
			else
			{
				if (args[1].Equals("match"))
				{
					Benchmark anBenchmark = (Benchmark) db.GetRoot();
					anBenchmark.matchQuery();
				}
			}
		}
		else
		{
			if (args[0].Equals("create"))
			{
				int scale = - 1;
				if (args[1].Equals("test3Conn"))
				{
					scale = fTest3Conn;
				}
				else if (args[1].Equals("test1Conn"))
				{
					scale = fTest1Conn;
				}
				else if (args[1].Equals("tiny"))
				{
					scale = fTiny;
				}
				else if (args[1].Equals("small"))
				{
					scale = fSmall;
				}
				else
				{
					System.Console.Out.WriteLine("Invalid scale");
					System.Environment.Exit(1);
				}
				Benchmark anBenchmark = new BenchmarkImpl();
				db.SetRoot(anBenchmark);
				anBenchmark.create(scale);
			}
		}
		
		System.Console.Out.WriteLine("time: " + ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + "msec");
		db.Close();
	}

	internal static void  printUsage()
	{
		System.Console.Out.WriteLine("usage: OO7 (create|query) [options]");
		System.Console.Out.WriteLine("    create options:");
		System.Console.Out.WriteLine("        size        - (tiny|small|large)");
		System.Console.Out.WriteLine("    query options:");
		System.Console.Out.WriteLine("        type        - (traversal|match)");
	}

	internal static int getRandomInt(int lower, int upper)
	{
		if (theRandom == null)
			theRandom = new System.Random();
		
		int rVal;
		do 
		{
			//UPGRADE_TODO: Method 'java.util.Random.nextInt' was converted to 'System.Random.Next' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073'"
			rVal = theRandom.Next();
			rVal %= upper;
			//System.out.println("rVal: " + rVal + " lower: " + lower + " upper: " + upper);
		}
		while (rVal < lower || rVal >= upper);
		return rVal;
	}

	public virtual void  create(int anScale)
	{
		theScale = anScale;
		createModule();
	}

	public virtual void  traversalQuery()
	{
		System.Collections.Hashtable table = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		long time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
		traversal(theModule.designRoot(), table);
		time = ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - time);
		System.Console.Out.WriteLine("Millis: " + time);
	}

	protected internal virtual void  traversal(OO7_Assembly anAssembly, System.Collections.Hashtable aTable)
	{
		if (anAssembly is OO7_BaseAssembly)
		{
			// System.out.println( "Base Assembly Class: " );
			OO7_BaseAssembly baseAssembly = (OO7_BaseAssembly) anAssembly;
			System.Collections.IEnumerator compIterator = baseAssembly.componentsShar().GetEnumerator();
			//UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilIteratorhasNext'"
			while (compIterator.MoveNext())
			{
				//UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilIteratornext'"
				OO7_CompositePart compositePart = (OO7_CompositePart) compIterator.Current;
				dfs(compositePart);
			}
		}
		else
		{
			OO7_ComplexAssembly complexAssembly = (OO7_ComplexAssembly) anAssembly;
			System.Collections.IEnumerator aIterator = complexAssembly.subAssemblies().GetEnumerator();
			//UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilIteratorhasNext'"
			while (aIterator.MoveNext())
			{
				//UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilIteratornext'"
				traversal((OO7_Assembly) aIterator.Current, aTable);
			}
		}
	}

	protected internal virtual void  dfs(OO7_CompositePart aPart)
	{
		System.Collections.Hashtable table = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		dfsVisit(aPart.rootPart(), table);
		//System.out.println( "AtomicParts visited: " + table.size() );
	}

	protected internal virtual void  dfsVisit(OO7_AtomicPart anAtomicPart, System.Collections.Hashtable aTable)
	{
		System.Collections.IEnumerator connIterator = anAtomicPart.from().GetEnumerator();
		//UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilIteratorhasNext'"
		while (connIterator.MoveNext())
		{
			//UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilIteratornext'"
			OO7_Connection connection = (OO7_Connection) connIterator.Current;
			OO7_AtomicPart part = connection.to();
			if (!aTable.ContainsKey(part))
			{
				aTable[part] = part;
				dfsVisit(part, aTable);
			}
		}
	}

	public virtual void  matchQuery()
	{
		int atomicParts = fNumAtomicPerComp[theScale] * fNumCompPerModule[theScale];
		long[] oids = new long[1000];
		int i;
		for (i = 0; i < oids.Length; ++i)
		{
			oids[i] = getRandomInt(0, atomicParts);
			//System.out.println( "oids[" + i + "] : " + oids[i] );
		}
		long time = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
		for (i = 0; i < oids.Length; ++i)
		{
			OO7_AtomicPart part = theModule.getAtomicPartByName("OO7_AtomicPart" + oids[i]);
		}
		time = ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 - time);
		System.Console.Out.WriteLine("Millis: " + time);
	}

	protected internal virtual void  createModule()
	{
		OO7_CompositePart[] compositeParts = new OO7_CompositePart[fNumCompPerModule[theScale]];
		theModule = new OO7_ModuleImpl(Storage);
		for (int i = 0; i < fNumCompPerModule[theScale]; ++i)
		{
			compositeParts[i] = createCompositePart();
		}
		OO7_ComplexAssembly designRoot = (OO7_ComplexAssembly) createAssembly(theModule, fNumAssmLevels[theScale], compositeParts);
		theModule.DesignRoot = designRoot;
		Modify();
	}

	protected internal virtual OO7_CompositePart createCompositePart()
	{
		// Document erzeugen
		OO7_Document document = new OO7_DocumentImpl();
		// CompositeParterzeugen
		OO7_CompositePart compositePart = new OO7_CompositePartImpl(Storage);
		if (verbose)
		{
			System.Console.Out.WriteLine("CompositePart created");
		}
		compositePart.Documentation = document;
		
		OO7_AtomicPart[] atomicParts = new OO7_AtomicPart[fNumAtomicPerComp[theScale]];
		// AtomicParts erzeugen
		for (int i = 0; i < fNumAtomicPerComp[theScale]; ++i)
		{
			long oid = AtomicPartOid;
			OO7_AtomicPart part = new OO7_AtomicPartImpl(Storage);
			if (verbose)
			{
				System.Console.Out.WriteLine("AtomicPart: " + oid + " created");
			}
			compositePart.addPart(part);
			part.PartOf = compositePart;
			theModule.addAtomicPart("OO7_AtomicPart" + oid, part);
			atomicParts[i] = part;
		}
		compositePart.RootPart = atomicParts[0];
		
		// AtomicParts miteinander verbinden
		for (int i = 0; i < fNumAtomicPerComp[theScale]; ++i)
		{
			int next = (i + 1) % fNumAtomicPerComp[theScale];
			OO7_Connection connection = new OO7_ConnectionImpl("");
			connection.From = atomicParts[i];
			atomicParts[i].addFrom(connection);
			connection.To = atomicParts[next];
			atomicParts[next].addTo(connection);
			if (verbose)
			{
				System.Console.Out.WriteLine("Connection: from: " + i + " to: " + next);
			}
			for (int j = 0; j < (fConnPerAtomic[theScale] - 1); ++j)
			{
				next = getRandomInt(0, fNumAtomicPerComp[theScale]);
				connection = new OO7_ConnectionImpl("");
				connection.From = atomicParts[j];
				atomicParts[j].addFrom(connection);
				connection.To = atomicParts[next];
				atomicParts[next].addTo(connection);
				if (verbose)
				{
					System.Console.Out.WriteLine("Connection: from: " + j + " to: " + next);
				}
			}
		}
		return compositePart;
	}

	protected internal virtual OO7_Assembly createAssembly(OO7_Module aModule, int aLevel, OO7_CompositePart[] someCompositeParts)
	{
		if (verbose)
		{
			System.Console.Out.WriteLine("level: " + aLevel);
		}
		if (aLevel == 1)
		{
			OO7_BaseAssembly baseAssembly = new OO7_BaseAssemblyImpl(Storage);
			aModule.addAssembly(baseAssembly);
			for (int j = 0; j < fNumCompPerAssm[theScale]; ++j)
			{
				int k = getRandomInt(0, fNumCompPerModule[theScale]);
				baseAssembly.addComponentsShar(someCompositeParts[k]);
			}
			return baseAssembly;
		}
		else
		{
			OO7_ComplexAssembly complexAssembly = new OO7_ComplexAssemblyImpl(Storage);
			aModule.addAssembly(complexAssembly);
			for (int i = 0; i < fNumAssmPerAssm[theScale]; ++i)
			{
				complexAssembly.addSubAssembly(createAssembly(aModule, aLevel - 1, someCompositeParts));
			}
			return complexAssembly;
		}
	}
}