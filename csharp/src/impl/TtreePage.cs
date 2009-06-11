namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    public class TtreePage : Persistent
    {
        internal const int maxItems = (Page.pageSize - ObjectHeader.Sizeof - 4 * 5) / 4;
        internal const int minItems = maxItems - 2; // minimal number of items in internal node

        internal TtreePage left;
        internal TtreePage right;
        internal int balance;
        internal int nItems;
        internal IPersistent[] item;

        internal class PageReference
        {
            internal TtreePage pg;

            internal PageReference(TtreePage p)
            {
                pg = p;
            }
        }

        public override bool RecursiveLoading
        {
            get
            {
                return false;
            }
        }

        internal TtreePage()
        {
        }

        internal TtreePage(IPersistent mbr)
        {
            nItems = 1;
            item = new IPersistent[maxItems];
            item[0] = mbr;
        }

        internal IPersistent LoadItem(int i)
        {
            IPersistent mbr = item[i];
            mbr.Load();
            return mbr;
        }

        internal bool Find(PersistentComparator comparator, object minValue, int minInclusive, object maxValue, int maxInclusive, ArrayList selection)
        {
            int l, r, m, n;
            Load();
            n = nItems;
            if (minValue != null)
            {
                if (-comparator.CompareMemberWithKey(LoadItem(0), minValue) >= minInclusive)
                {
                    if (-comparator.CompareMemberWithKey(LoadItem(n - 1), minValue) >= minInclusive)
                    {
                        if (right != null)
                        {
                            return right.Find(comparator, minValue, minInclusive, maxValue, maxInclusive, selection);
                        }
                        return true;
                    }

                    for (l = 0, r = n; l < r; )
                    {
                        m = (l + r) >> 1;
                        if (-comparator.CompareMemberWithKey(LoadItem(m), minValue) >= minInclusive)
                        {
                            l = m + 1;
                        }
                        else
                        {
                            r = m;
                        }
                    }

                    while (r < n)
                    {
                        if (maxValue != null && comparator.CompareMemberWithKey(LoadItem(r), maxValue) >= maxInclusive)
                        {
                            return false;
                        }
                        selection.Add(LoadItem(r));
                        r += 1;
                    }

                    if (right != null)
                    {
                        return right.Find(comparator, minValue, minInclusive, maxValue, maxInclusive, selection);
                    }
                    return true;
                }
            }

            if (left != null)
            {
                if (!left.Find(comparator, minValue, minInclusive, maxValue, maxInclusive, selection))
                {
                    return false;
                }
            }

            for (l = 0; l < n; l++)
            {
                if (maxValue != null && comparator.CompareMemberWithKey(LoadItem(l), maxValue) >= maxInclusive)
                {
                    return false;
                }
                selection.Add(LoadItem(l));
            }

            if (right != null)
            {
                return right.Find(comparator, minValue, minInclusive, maxValue, maxInclusive, selection);
            }

            return true;
        }

        internal bool Contains(PersistentComparator comparator, IPersistent mbr)
        {
            int l, r, m, n;
            Load();
            n = nItems;
            if (comparator.CompareMembers(LoadItem(0), mbr) < 0)
            {
                if (comparator.CompareMembers(LoadItem(n - 1), mbr) < 0)
                {
                    if (right != null)
                    {
                        return right.Contains(comparator, mbr);
                    }
                    return false;
                }

                for (l = 0, r = n; l < r; )
                {
                    m = (l + r) >> 1;
                    if (comparator.CompareMembers(LoadItem(m), mbr) < 0)
                    {
                        l = m + 1;
                    }
                    else
                    {
                        r = m;
                    }
                }

                while (r < n)
                {
                    if (mbr == LoadItem(r))
                    {
                        return true;
                    }
                    if (comparator.CompareMembers(item[r], mbr) > 0)
                    {
                        return false;
                    }
                    r += 1;
                }
                if (right != null)
                {
                    return right.Contains(comparator, mbr);
                }
                return false;
            }

            if (left != null)
            {
                if (left.Contains(comparator, mbr))
                {
                    return true;
                }
            }

            for (l = 0; l < n; l++)
            {
                if (mbr == LoadItem(l))
                {
                    return true;
                }
                if (comparator.CompareMembers(item[l], mbr) > 0)
                {
                    return false;
                }
            }

            if (right != null)
            {
                return right.Contains(comparator, mbr);
            }

            return false;
        }

        internal const int OK = 0;
        internal const int NOT_UNIQUE = 1;
        internal const int NOT_FOUND = 2;
        internal const int OVERFLOW = 3;
        internal const int UNDERFLOW = 4;

        internal int Insert(PersistentComparator comparator, IPersistent mbr, bool unique, PageReference ref_Renamed)
        {
            Load();
            int n = nItems;
            TtreePage pg;
            int diff = comparator.CompareMembers(mbr, LoadItem(0));
            if (diff <= 0)
            {
                if (unique && diff == 0)
                {
                    return NOT_UNIQUE;
                }
                if ((this.left == null || diff == 0) && n != maxItems)
                {
                    Modify();
                    //for (int i = n; i > 0; i--) item[i] = item[i-1];
                    Array.Copy(item, 0, item, 1, n);
                    item[0] = mbr;
                    nItems += 1;
                    return OK;
                }
                if (this.left == null)
                {
                    Modify();
                    this.left = new TtreePage(mbr);
                }
                else
                {
                    pg = ref_Renamed.pg;
                    ref_Renamed.pg = this.left;
                    int result = this.left.Insert(comparator, mbr, unique, ref_Renamed);
                    if (result == NOT_UNIQUE)
                    {
                        return NOT_UNIQUE;
                    }
                    Modify();
                    this.left = ref_Renamed.pg;
                    ref_Renamed.pg = pg;
                    if (result == OK)
                        return OK;
                }

                if (balance > 0)
                {
                    balance = 0;
                    return OK;
                }
                else if (balance == 0)
                {
                    balance = -1;
                    return OVERFLOW;
                }
                else
                {
                    TtreePage left = this.left;
                    left.Load();
                    left.Modify();
                    if (left.balance < 0)
                    {
                        // single LL turn
                        this.left = left.right;
                        left.right = this;
                        balance = 0;
                        left.balance = 0;
                        ref_Renamed.pg = left;
                    }
                    else
                    {
                        // double LR turn
                        TtreePage right = left.right;
                        right.Load();
                        right.Modify();
                        left.right = right.left;
                        right.left = left;
                        this.left = right.right;
                        right.right = this;
                        balance = (right.balance < 0) ? 1 : 0;
                        left.balance = (right.balance > 0) ? -1 : 0;
                        right.balance = 0;
                        ref_Renamed.pg = right;
                    }
                    return OK;
                }
            }

            diff = comparator.CompareMembers(mbr, LoadItem(n - 1));
            if (diff >= 0)
            {
                if (unique && diff == 0)
                {
                    return NOT_UNIQUE;
                }

                if ((this.right == null || diff == 0) && n != maxItems)
                {
                    Modify();
                    item[n] = mbr;
                    nItems += 1;
                    return OK;
                }
                if (this.right == null)
                {
                    Modify();
                    this.right = new TtreePage(mbr);
                }
                else
                {
                    pg = ref_Renamed.pg;
                    ref_Renamed.pg = this.right;
                    int result = this.right.Insert(comparator, mbr, unique, ref_Renamed);
                    if (result == NOT_UNIQUE)
                    {
                        return NOT_UNIQUE;
                    }
                    Modify();
                    this.right = ref_Renamed.pg;
                    ref_Renamed.pg = pg;
                    if (result == OK)
                        return OK;
                }

                if (balance < 0)
                {
                    balance = 0;
                    return OK;
                }
                else if (balance == 0)
                {
                    balance = 1;
                    return OVERFLOW;
                }
                else
                {
                    TtreePage right = this.right;
                    right.Load();
                    right.Modify();
                    if (right.balance > 0)
                    {
                        // single RR turn
                        this.right = right.left;
                        right.left = this;
                        balance = 0;
                        right.balance = 0;
                        ref_Renamed.pg = right;
                    }
                    else
                    {
                        // double RL turn
                        TtreePage left = right.left;
                        left.Load();
                        left.Modify();
                        right.left = left.right;
                        left.right = right;
                        this.right = left.left;
                        left.left = this;
                        balance = (left.balance > 0) ? -1 : 0;
                        right.balance = (left.balance < 0) ? 1 : 0;
                        left.balance = 0;
                        ref_Renamed.pg = left;
                    }
                    return OK;
                }
            }

            int l = 1, r = n - 1;
            while (l < r)
            {
                int i = (l + r) >> 1;
                diff = comparator.CompareMembers(mbr, LoadItem(i));
                if (diff > 0)
                {
                    l = i + 1;
                }
                else
                {
                    r = i;
                    if (diff == 0)
                    {
                        if (unique)
                        {
                            return NOT_UNIQUE;
                        }
                        break;
                    }
                }
            }
            // Insert before item[r]
            Modify();
            if (n != maxItems)
            {
                Array.Copy(item, r, item, r + 1, n - r);
                //for (int i = n; i > r; i--) item[i] = item[i-1];
                item[r] = mbr;
                nItems += 1;
                return OK;
            }
            else
            {
                IPersistent reinsertItem;
                if (balance >= 0)
                {
                    reinsertItem = LoadItem(0);
                    Array.Copy(item, 1, item, 0, r - 1);
                    //for (int i = 1; i < r; i++) item[i-1] = item[i];
                    item[r - 1] = mbr;
                }
                else
                {
                    reinsertItem = LoadItem(n - 1);
                    Array.Copy(item, r, item, r + 1, n - r - 1);
                    //for (int i = n-1; i > r; i--) item[i] = item[i-1];
                    item[r] = mbr;
                }
                return Insert(comparator, reinsertItem, unique, ref_Renamed);
            }
        }

        internal int BalanceLeftBranch(PageReference ref_Renamed)
        {
            if (balance < 0)
            {
                balance = 0;
                return UNDERFLOW;
            }
            else if (balance == 0)
            {
                balance = 1;
                return OK;
            }
            else
            {
                TtreePage right = this.right;
                right.Load();
                right.Modify();
                if (right.balance >= 0)
                {
                    // single RR turn
                    this.right = right.left;
                    right.left = this;
                    if (right.balance == 0)
                    {
                        this.balance = 1;
                        right.balance = -1;
                        ref_Renamed.pg = right;
                        return OK;
                    }
                    else
                    {
                        balance = 0;
                        right.balance = 0;
                        ref_Renamed.pg = right;
                        return UNDERFLOW;
                    }
                }
                else
                {
                    // double RL turn
                    TtreePage left = right.left;
                    left.Load();
                    left.Modify();
                    right.left = left.right;
                    left.right = right;
                    this.right = left.left;
                    left.left = this;
                    balance = left.balance > 0 ? -1 : 0;
                    right.balance = left.balance < 0 ? 1 : 0;
                    left.balance = 0;
                    ref_Renamed.pg = left;
                    return UNDERFLOW;
                }
            }
        }

        internal int BalanceRightBranch(PageReference ref_Renamed)
        {
            if (balance > 0)
            {
                balance = 0;
                return UNDERFLOW;
            }
            else if (balance == 0)
            {
                balance = -1;
                return OK;
            }
            else
            {
                TtreePage left = this.left;
                left.Load();
                left.Modify();
                if (left.balance <= 0)
                {
                    // single LL turn
                    this.left = left.right;
                    left.right = this;
                    if (left.balance == 0)
                    {
                        balance = -1;
                        left.balance = 1;
                        ref_Renamed.pg = left;
                        return OK;
                    }
                    else
                    {
                        balance = 0;
                        left.balance = 0;
                        ref_Renamed.pg = left;
                        return UNDERFLOW;
                    }
                }
                else
                {
                    // double LR turn
                    TtreePage right = left.right;
                    right.Load();
                    right.Modify();
                    left.right = right.left;
                    right.left = left;
                    this.left = right.right;
                    right.right = this;
                    balance = right.balance < 0 ? 1 : 0;
                    left.balance = right.balance > 0 ? -1 : 0;
                    right.balance = 0;
                    ref_Renamed.pg = right;
                    return UNDERFLOW;
                }
            }
        }

        internal int Remove(PersistentComparator comparator, IPersistent mbr, PageReference ref_Renamed)
        {
            Load();
            TtreePage pg;
            int n = nItems;
            int diff = comparator.CompareMembers(mbr, LoadItem(0));
            if (diff <= 0)
            {
                if (left != null)
                {
                    Modify();
                    pg = ref_Renamed.pg;
                    ref_Renamed.pg = left;
                    int h = left.Remove(comparator, mbr, ref_Renamed);
                    left = ref_Renamed.pg;
                    ref_Renamed.pg = pg;
                    if (h == UNDERFLOW)
                    {
                        return BalanceLeftBranch(ref_Renamed);
                    }
                    else if (h == OK)
                    {
                        return OK;
                    }
                }
            }
            diff = comparator.CompareMembers(mbr, LoadItem(n - 1));
            if (diff <= 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (item[i] == mbr)
                    {
                        if (n == 1)
                        {
                            if (right == null)
                            {
                                Deallocate();
                                ref_Renamed.pg = left;
                                return UNDERFLOW;
                            }
                            else if (left == null)
                            {
                                Deallocate();
                                ref_Renamed.pg = right;
                                return UNDERFLOW;
                            }
                        }
                        Modify();
                        if (n <= minItems)
                        {
                            if (left != null && balance <= 0)
                            {
                                TtreePage prev = left;
                                prev.Load();
                                while (prev.right != null)
                                {
                                    prev = prev.right;
                                    prev.Load();
                                }
                                Array.Copy(item, 0, item, 1, i);
                                //while (--i >= 0) {
                                //    item[i+1] = item[i];
                                //}
                                item[0] = prev.item[prev.nItems - 1];
                                pg = ref_Renamed.pg;
                                ref_Renamed.pg = left;
                                int h = left.Remove(comparator, LoadItem(0), ref_Renamed);
                                left = ref_Renamed.pg;
                                ref_Renamed.pg = pg;
                                if (h == UNDERFLOW)
                                {
                                    h = BalanceLeftBranch(ref_Renamed);
                                }
                                return h;
                            }
                            else if (right != null)
                            {
                                TtreePage next = right;
                                next.Load();
                                while (next.left != null)
                                {
                                    next = next.left;
                                    next.Load();
                                }
                                Array.Copy(item, i + 1, item, i, n - i - 1);
                                //while (++i < n) {
                                //    item[i-1] = item[i];
                                //}
                                item[n - 1] = next.item[0];
                                pg = ref_Renamed.pg;
                                ref_Renamed.pg = right;
                                int h = right.Remove(comparator, LoadItem(n - 1), ref_Renamed);
                                right = ref_Renamed.pg;
                                ref_Renamed.pg = pg;
                                if (h == UNDERFLOW)
                                {
                                    h = BalanceRightBranch(ref_Renamed);
                                }
                                return h;
                            }
                        }
                        Array.Copy(item, i + 1, item, i, n - i - 1);
                        //while (++i < n) {
                        //    item[i-1] = item[i];
                        //}
                        item[n - 1] = null;
                        nItems -= 1;
                        return OK;
                    }
                }
            }

            if (right != null)
            {
                Modify();
                pg = ref_Renamed.pg;
                ref_Renamed.pg = right;
                int h = right.Remove(comparator, mbr, ref_Renamed);
                right = ref_Renamed.pg;
                ref_Renamed.pg = pg;
                if (h == UNDERFLOW)
                {
                    return BalanceRightBranch(ref_Renamed);
                }
                else
                {
                    return h;
                }
            }
            return NOT_FOUND;
        }

        internal int ToArray(IPersistent[] arr, int index)
        {
            Load();
            if (left != null)
            {
                index = left.ToArray(arr, index);
            }
            for (int i = 0, n = nItems; i < n; i++)
            {
                arr[index++] = LoadItem(i);
            }
            if (right != null)
            {
                index = right.ToArray(arr, index);
            }
            return index;
        }

        internal void Prune()
        {
            Load();
            if (left != null)
            {
                left.Prune();
            }
            if (right != null)
            {
                right.Prune();
            }
            Deallocate();
        }
    }
}
