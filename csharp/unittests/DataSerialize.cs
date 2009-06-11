namespace TenderBaseTest
{
    using System;
	using System.IO;
    using NUnit.Framework;
    using NAssert = NUnit.Framework.Assert;
    using TenderBase;
    using TenderBaseImpl;

	public class Data : Persistent
	{
		byte	ByteVal;
		int		IntVal;
		long	LongVal;
		string	StrVal;

		public Data() {}

		public Data(byte val)
		{
			SetVal(val);
		}

		public void SetVal(byte val)
		{
			ByteVal = val;
			IntVal = val;
			LongVal = val;
			StrVal = val.ToString();
		}

		public bool IsVal(byte val)
		{
			if (ByteVal != val)
				return false;
			if (IntVal != val)
				return false;
			if (LongVal != val)
				return false;
			if (val.ToString() != StrVal)
				return false;
			return true;
		}
	}

    [TestFixture]
    public class DataSerializeTest
    {
		static string DatabaseName = "DataSerialize.dbs";
	    const int pagePoolSize = 32 * 1024 * 1024;

        [SetUp]
        public void Init()
        {
			if (System.IO.File.Exists(DatabaseName))
				System.IO.File.Delete(DatabaseName);
        }

        [Test]
        public void Test00()
        {
	        Storage db = StorageFactory.Instance.CreateStorage();
	        db.Open(DatabaseName, pagePoolSize);
	        Data root = (Data) db.GetRoot();
			NAssert.AreEqual(root, null);
			root = new Data(1);
			db.SetRoot(root);
			db.Commit();

			Data data2 = (Data) db.GetRoot();
			NAssert.AreEqual(data2.IsVal(1), true);
        }
	}
}
