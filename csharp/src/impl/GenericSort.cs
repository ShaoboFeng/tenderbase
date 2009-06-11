namespace TenderBaseImpl
{
    using System;

    public class GenericSort
    {
        internal static void Sort(GenericSortArray arr)
        {
            Sort1(arr, 0, arr.Size());
        }

        private static void Sort1(GenericSortArray x, int off, int len)
        {
            // Insertion sort on smallest arrays
            if (len < 7)
            {
                for (int i = off; i < len + off; i++)
                {
                    for (int j = i; j > off && x.Compare(j - 1, j) > 0; j--)
                    {
                        x.Swap(j, j - 1);
                    }
                }
                return;
            }

            // Choose a partition element, v
            int m = off + (len >> 1); // Small arrays, middle element
            if (len > 7)
            {
                int l = off;
                int n = off + len - 1;
                if (len > 40)
                {
                    // Big arrays, pseudomedian of 9
                    int s = len / 8;
                    l = Med3(x, l, l + s, l + 2 * s);
                    m = Med3(x, m - s, m, m + s);
                    n = Med3(x, n - 2 * s, n - s, n);
                }
                m = Med3(x, l, m, n); // Mid-size, med of 3
            }
            // Establish Invariant: v* (<v)* (>v)* v*
            int a = off, b = a, c = off + len - 1, d = c, diff;
            while (true)
            {
                while (b <= c && (diff = x.Compare(b, m)) <= 0)
                {
                    if (diff == 0)
                    {
                        x.Swap(a++, b);
                    }
                    b++;
                }
                while (c >= b && (diff = x.Compare(c, m)) >= 0)
                {
                    if (diff == 0)
                    {
                        x.Swap(c, d--);
                    }
                    c--;
                }
                if (b > c)
                {
                    break;
                }
                x.Swap(b++, c--);
            }

            // Swap partition elements back to middle
            int s2, n2 = off + len;
            s2 = System.Math.Min(a - off, b - a); VecSwap(x, off, b - s2, s2);
            s2 = System.Math.Min(d - c, n2 - d - 1); VecSwap(x, b, n2 - s2, s2);

            // Recursively sort non-partition-elements
            if ((s2 = b - a) > 1)
            {
                Sort1(x, off, s2);
            }
            if ((s2 = d - c) > 1)
            {
                Sort1(x, n2 - s2, s2);
            }
        }

        private static void VecSwap(GenericSortArray x, int a, int b, int n)
        {
            for (int i = 0; i < n; i++, a++, b++)
            {
                x.Swap(a, b);
            }
        }

        private static int Med3(GenericSortArray x, int a, int b, int c)
        {
            return (x.Compare(a, b) < 0 ? (x.Compare(b, c) < 0 ? b : (x.Compare(a, c) < 0 ? c : a)) : (x.Compare(b, c) > 0 ? b : (x.Compare(a, c) > 0 ? c : a)));
        }
    }
}

