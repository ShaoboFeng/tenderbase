#if !OMIT_RTREE
namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    public class RtreePage : Persistent
    {
        internal const int card = (Page.pageSize - ObjectHeader.Sizeof - 4 * 3) / (4 * 4 + 4);
        internal const int minFill = card / 2;

        internal int n;
        internal Rectangle[] b;
        internal Link branch;

        internal RtreePage(Storage storage, IPersistent obj, Rectangle r)
        {
            branch = storage.CreateLink(card);
            branch.Size = card;
            b = new Rectangle[card];
            SetBranch(0, new Rectangle(r), obj);
            n = 1;
            for (int i = 1; i < card; i++)
            {
                b[i] = new Rectangle();
            }
        }

        internal RtreePage(Storage storage, RtreePage root, RtreePage p)
        {
            branch = storage.CreateLink(card);
            branch.Size = card;
            b = new Rectangle[card];
            n = 2;
            SetBranch(0, root.Cover(), root);
            SetBranch(1, p.Cover(), p);
            for (int i = 2; i < card; i++)
            {
                b[i] = new Rectangle();
            }
        }

        internal RtreePage()
        {
        }

        internal virtual RtreePage Insert(Storage storage, Rectangle r, IPersistent obj, int level)
        {
            Modify();
            if (--level != 0)
            {
                // not leaf page
                int i, mini = 0;
                long minIncr = Int64.MaxValue;
                long minArea = Int64.MaxValue;
                for (i = 0; i < n; i++)
                {
                    long area = b[i].Area();
                    long incr = Rectangle.JoinArea(b[i], r) - area;
                    if (incr < minIncr)
                    {
                        minIncr = incr;
                        minArea = area;
                        mini = i;
                    }
                    else if (incr == minIncr && area < minArea)
                    {
                        minArea = area;
                        mini = i;
                    }
                }

                RtreePage p = (RtreePage) branch.Get(mini);
                RtreePage q = p.Insert(storage, r, obj, level);
                if (q == null)
                {
                    // child was not split
                    b[mini].Join(r);
                    return null;
                }
                else
                {
                    // child was split
                    SetBranch(mini, p.Cover(), p);
                    return AddBranch(storage, q.Cover(), q);
                }
            }
            else
            {
                return AddBranch(storage, new Rectangle(r), obj);
            }
        }

        internal virtual int Remove(Rectangle r, IPersistent obj, int level, ArrayList reinsertList)
        {
            if (--level != 0)
            {
                for (int i = 0; i < n; i++)
                {
                    if (r.Intersects(b[i]))
                    {
                        RtreePage pg = (RtreePage) branch.Get(i);
                        int reinsertLevel = pg.Remove(r, obj, level, reinsertList);
                        if (reinsertLevel >= 0)
                        {
                            if (pg.n >= minFill)
                            {
                                SetBranch(i, pg.Cover(), pg);
                                Modify();
                            }
                            else
                            {
                                // not enough entries in child
                                reinsertList.Add(pg);
                                reinsertLevel = level - 1;
                                RemoveBranch(i);
                            }
                            return reinsertLevel;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    if (branch.ContainsElement(i, obj))
                    {
                        RemoveBranch(i);
                        return 0;
                    }
                }
            }
            return -1;
        }

        internal virtual void Find(Rectangle r, ArrayList result, int level)
        {
            if (--level != 0)
            {
                /* this is an internal node in the tree */
                for (int i = 0; i < n; i++)
                {
                    if (r.Intersects(b[i]))
                    {
                        ((RtreePage) branch.Get(i)).Find(r, result, level);
                    }
                }
            }
            else
            {
                /* this is a leaf node */
                for (int i = 0; i < n; i++)
                {
                    if (r.Intersects(b[i]))
                    {
                        result.Add(branch.Get(i));
                    }
                }
            }
        }

        internal virtual void Purge(int level)
        {
            if (--level != 0)
            {
                /* this is an internal node in the tree */
                for (int i = 0; i < n; i++)
                {
                    ((RtreePage) branch.Get(i)).Purge(level);
                }
            }
            Deallocate();
        }

        internal void SetBranch(int i, Rectangle r, IPersistent obj)
        {
            b[i] = r;
            branch.Set(i, obj);
        }

        internal void RemoveBranch(int i)
        {
            n -= 1;
            Array.Copy(b, i + 1, b, i, n - i);
            branch.Remove(i);
            branch.Size = card;
            Modify();
        }

        internal RtreePage AddBranch(Storage storage, Rectangle r, IPersistent obj)
        {
            if (n < card)
            {
                SetBranch(n++, r, obj);
                return null;
            }
            else
            {
                return SplitPage(storage, r, obj);
            }
        }

        internal RtreePage SplitPage(Storage storage, Rectangle r, IPersistent obj)
        {
            int i, j, seed0 = 0, seed1 = 0;
            long[] rectArea = new long[card + 1];
            long waste;
            long worstWaste = Int64.MinValue;
            //
            // As the seeds for the two groups, find two rectangles which waste
            // the most area if covered by a single rectangle.
            //
            rectArea[0] = r.Area();
            for (i = 0; i < card; i++)
            {
                rectArea[i + 1] = b[i].Area();
            }
            Rectangle bp = r;
            for (i = 0; i < card; i++)
            {
                for (j = i + 1; j <= card; j++)
                {
                    waste = Rectangle.JoinArea(bp, b[j - 1]) - rectArea[i] - rectArea[j];
                    if (waste > worstWaste)
                    {
                        worstWaste = waste;
                        seed0 = i;
                        seed1 = j;
                    }
                }
                bp = b[i];
            }
            byte[] taken = new byte[card];
            Rectangle group0, group1;
            long groupArea0, groupArea1;
            int groupCard0, groupCard1;
            RtreePage pg;

            taken[seed1 - 1] = 2;
            group1 = new Rectangle(b[seed1 - 1]);

            if (seed0 == 0)
            {
                group0 = new Rectangle(r);
                pg = new RtreePage(storage, obj, r);
            }
            else
            {
                group0 = new Rectangle(b[seed0 - 1]);
                pg = new RtreePage(storage, branch.GetRaw(seed0 - 1), group0);
                SetBranch(seed0 - 1, r, obj);
            }
            groupCard0 = groupCard1 = 1;
            groupArea0 = rectArea[seed0];
            groupArea1 = rectArea[seed1];
            //
            // Split remaining rectangles between two groups.
            // The one chosen is the one with the greatest difference in area
            // expansion depending on which group - the rect most strongly
            // attracted to one group and repelled from the other.
            //
            while (groupCard0 + groupCard1 < card + 1 && groupCard0 < card + 1 - minFill && groupCard1 < card + 1 - minFill)
            {
                int betterGroup = -1, chosen = -1;
                long biggestDiff = -1;
                for (i = 0; i < card; i++)
                {
                    if (taken[i] == 0)
                    {
                        long diff = (Rectangle.JoinArea(group0, b[i]) - groupArea0) - (Rectangle.JoinArea(group1, b[i]) - groupArea1);
                        if (diff > biggestDiff || -diff > biggestDiff)
                        {
                            chosen = i;
                            if (diff < 0)
                            {
                                betterGroup = 0;
                                biggestDiff = -diff;
                            }
                            else
                            {
                                betterGroup = 1;
                                biggestDiff = diff;
                            }
                        }
                    }
                }
                Assert.That(chosen >= 0);
                if (betterGroup == 0)
                {
                    group0.Join(b[chosen]);
                    groupArea0 = group0.Area();
                    taken[chosen] = 1;
                    pg.SetBranch(groupCard0++, b[chosen], branch.GetRaw(chosen));
                }
                else
                {
                    groupCard1 += 1;
                    group1.Join(b[chosen]);
                    groupArea1 = group1.Area();
                    taken[chosen] = 2;
                }
            }

            // If one group gets too full, then remaining rectangle are
            // split between two groups in such way to balance cards of two groups.
            if (groupCard0 + groupCard1 < card + 1)
            {
                for (i = 0; i < card; i++)
                {
                    if (taken[i] == 0)
                    {
                        if (groupCard0 >= groupCard1)
                        {
                            taken[i] = 2;
                            groupCard1 += 1;
                        }
                        else
                        {
                            taken[i] = 1;
                            pg.SetBranch(groupCard0++, b[i], branch.GetRaw(i));
                        }
                    }
                }
            }
            pg.n = groupCard0;
            n = groupCard1;
            for (i = 0, j = 0; i < groupCard1; j++)
            {
                if (taken[j] == 2)
                {
                    SetBranch(i++, b[j], branch.GetRaw(j));
                }
            }
            return pg;
        }

        internal Rectangle Cover()
        {
            Rectangle r = new Rectangle(b[0]);
            for (int i = 1; i < n; i++)
            {
                r.Join(b[i]);
            }
            return r;
        }
    }
}
#endif
