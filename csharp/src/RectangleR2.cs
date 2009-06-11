#if !OMIT_RTREER2
namespace TenderBase
{
    using System;
    
    /// <summary> R2 rectangle class. This class is used in spatial index.</summary>
    public class RectangleR2 : IValue, System.ICloneable
    {
        /// <summary> Smallest Y coordinate of the rectangle</summary>
        public virtual double Top
        {
            get
            {
                return top;
            }
        }

        /// <summary> Smallest X coordinate of the rectangle</summary>
        public virtual double Left
        {
            get
            {
                return left;
            }
        }

        /// <summary> Greatest Y coordinate of the rectangle</summary>
        public virtual double Bottom
        {
            get
            {
                return bottom;
            }
        }

        /// <summary> Greatest X coordinate of the rectangle</summary>
        public virtual double Right
        {
            get
            {
                return right;
            }
        }

        private double top;
        private double left;
        private double bottom;
        private double right;

        /// <summary> Rectangle area</summary>
        public double Area()
        {
            return (bottom - top) * (right - left);
        }

        /// <summary> Area of covered rectangle for two sepcified rectangles</summary>
        public static double JoinArea(RectangleR2 a, RectangleR2 b)
        {
            double left = (a.left < b.left) ? a.left : b.left;
            double right = (a.right > b.right) ? a.right : b.right;
            double top = (a.top < b.top) ? a.top : b.top;
            double bottom = (a.bottom > b.bottom) ? a.bottom : b.bottom;
            return (bottom - top) * (right - left);
        }

        /// <summary> Clone rectangle </summary>
        public virtual object Clone()
        {
            try
            {
                RectangleR2 r = (RectangleR2) base.MemberwiseClone();
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
        public RectangleR2(RectangleR2 r)
        {
            this.top = r.top;
            this.left = r.left;
            this.bottom = r.bottom;
            this.right = r.right;
        }

        /// <summary> Construct rectangle with specified coordinates</summary>
        public RectangleR2(double top, double left, double bottom, double right)
        {
            Assert.That(top <= bottom && left <= right);
            this.top = top;
            this.left = left;
            this.bottom = bottom;
            this.right = right;
        }

        /// <summary> Default constructor for PERST</summary>
        public RectangleR2()
        {
        }

        /// <summary> Join two rectangles. This rectangle is updates to contain Cover of this and specified rectangle.</summary>
        /// <param name="r">rectangle to be joined with this rectangle
        /// </param>
        public void Join(RectangleR2 r)
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
        public static RectangleR2 Join(RectangleR2 a, RectangleR2 b)
        {
            RectangleR2 r = new RectangleR2(a);
            r.Join(b);
            return r;
        }

        /// <summary> Checks if this rectangle intersects with specified rectangle</summary>
        public bool Intersects(RectangleR2 r)
        {
            return left <= r.right && top <= r.bottom && right >= r.left && bottom >= r.top;
        }

        /// <summary> Checks if this rectangle contains the specified rectangle</summary>
        public bool Contains(RectangleR2 r)
        {
            return left <= r.left && top <= r.top && right >= r.right && bottom >= r.bottom;
        }

        /// <summary> Check if two rectangles are equal</summary>
        public override bool Equals(object o)
        {
            if (o is RectangleR2)
            {
                RectangleR2 r = (RectangleR2) o;
                return left == r.left && top == r.top && right == r.right && bottom == r.bottom;
            }
            return false;
        }

        /// <summary> Hash code consists of all rectangle coordinates</summary>
        public override int GetHashCode()
        {
            long topLong = BitConverter.DoubleToInt64Bits(top);
            long bottomLong = BitConverter.DoubleToInt64Bits(bottom);
            long leftLong = BitConverter.DoubleToInt64Bits(left);
            long rightLong = BitConverter.DoubleToInt64Bits(right);
            long hash = topLong ^ (bottomLong << 1) ^ (leftLong << 2) ^ (rightLong << 3);
            return (int)hash;
        }

        public override string ToString()
        {
            return "top=" + top + ", left=" + left + ", bottom=" + bottom + ", right=" + right;
        }
    }
}
#endif
