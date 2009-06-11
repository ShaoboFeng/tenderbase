#if !OMIT_RTREE
namespace TenderBase
{
    using System;
    
    /// <summary> Rectangle with integer cooordinates. This class is used in spatial index.</summary>
    public class Rectangle : IValue, System.ICloneable
    {
        /// <summary> Smallest Y coordinate of the rectangle</summary>
        public virtual int Top
        {
            get
            {
                return top;
            }
        }

        /// <summary> Smallest X coordinate of the rectangle</summary>
        public virtual int Left
        {
            get
            {
                return left;
            }
        }

        /// <summary> Greatest Y coordinate of the rectangle</summary>
        public virtual int Bottom
        {
            get
            {
                return bottom;
            }
        }

        /// <summary> Greatest X coordinate of the rectangle</summary>
        public virtual int Right
        {
            get
            {
                return right;
            }
        }

        private int top;
        private int left;
        private int bottom;
        private int right;

        /// <summary> Rectangle area</summary>
        public long Area()
        {
            return (long) (bottom - top) * (right - left);
        }

        /// <summary> Area of covered rectangle for two sepcified rectangles</summary>
        public static long JoinArea(Rectangle a, Rectangle b)
        {
            int left = (a.left < b.left) ? a.left : b.left;
            int right = (a.right > b.right) ? a.right : b.right;
            int top = (a.top < b.top) ? a.top : b.top;
            int bottom = (a.bottom > b.bottom) ? a.bottom : b.bottom;
            return (long) (bottom - top) * (right - left);
        }

        /// <summary> Clone rectangle </summary>
        public virtual object Clone()
        {
            try
            {
                Rectangle r = (Rectangle) base.MemberwiseClone();
                r.top = this.top;
                r.left = this.left;
                r.bottom = this.bottom;
                r.right = this.right;
                return r;
            }
            //UPGRADE_NOTE: Exception 'java.lang.CloneNotSupportedException' was converted to 'System.Exception' which has different behavior.
            catch (System.Exception)
            {
                // this shouldn't happen, since we are Cloneable
                throw new System.ApplicationException();
            }
        }

        /// <summary> Create copy of the rectangle</summary>
        public Rectangle(Rectangle r)
        {
            this.top = r.top;
            this.left = r.left;
            this.bottom = r.bottom;
            this.right = r.right;
        }

        /// <summary> Construct rectangle with specified coordinates</summary>
        public Rectangle(int top, int left, int bottom, int right)
        {
            Assert.That(top <= bottom && left <= right);
            this.top = top;
            this.left = left;
            this.bottom = bottom;
            this.right = right;
        }

        /// <summary> Default constructor for PERST</summary>
        public Rectangle()
        {
        }

        /// <summary> Join two rectangles. This rectangle is updates to contain Cover of this and specified rectangle.</summary>
        /// <param name="r">rectangle to be joined with this rectangle
        /// </param>
        public void Join(Rectangle r)
        {
            if (left > r.left)
            {
                left = r.left;
            }
            if (right < r.right)
            {
                right = r.right;
            }
            if (top > r.top)
            {
                top = r.top;
            }
            if (bottom < r.bottom)
            {
                bottom = r.bottom;
            }
        }

        /// <summary> Non destructive join of two rectangles. </summary>
        /// <param name="a">first joined rectangle
        /// </param>
        /// <param name="b">second joined rectangle
        /// </param>
        /// <returns> rectangle containing Cover of these two rectangles
        /// </returns>
        public static Rectangle Join(Rectangle a, Rectangle b)
        {
            Rectangle r = new Rectangle(a);
            r.Join(b);
            return r;
        }

        /// <summary> Checks if this rectangle intersects with specified rectangle</summary>
        public bool Intersects(Rectangle r)
        {
            return left <= r.right && top <= r.bottom && right >= r.left && bottom >= r.top;
        }

        /// <summary> Checks if this rectangle contains the specified rectangle</summary>
        public bool Contains(Rectangle r)
        {
            return left <= r.left && top <= r.top && right >= r.right && bottom >= r.bottom;
        }

        /// <summary> Check if two rectangles are equal</summary>
        public override bool Equals(object o)
        {
            if (o is Rectangle)
            {
                Rectangle r = (Rectangle) o;
                return left == r.left && top == r.top && right == r.right && bottom == r.bottom;
            }
            return false;
        }

        /// <summary> Hash code consists of all rectangle coordinates</summary>
        public override int GetHashCode()
        {
            return top ^ (bottom << 1) ^ (left << 2) ^ (right << 3);
        }

        public override string ToString()
        {
            return "top=" + top + ", left=" + left + ", bottom=" + bottom + ", right=" + right;
        }
    }
}
#endif

