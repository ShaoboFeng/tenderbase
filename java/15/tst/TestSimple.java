import org.garret.perst.*;

class RecordTestSimple extends Persistent { 
    String strKey;
    long   intKey;
};

public class TestSimple {
    static int pagePoolSize = 32*1024*1024;

	static public void main(String[] args) {
        Storage db = StorageFactory.getInstance().createStorage();
        db.open("testsimple.dbs", pagePoolSize);
        long key = 1999;
        RecordTestSimple root = (RecordTestSimple)db.getRoot();
        if (root == null)
        {
        	RecordTestSimple rec = new RecordTestSimple();
            key = (3141592621L*key + 2718281829L) % 1000000007L;
            rec.intKey = key;
            rec.strKey = Long.toString(key);
            db.setRoot(rec);
            db.commit();
        }
        else
        {
        	System.out.println("intKey = " + root.intKey + " strKey = " + root.strKey);        	
        }
        db.close();
    }
}