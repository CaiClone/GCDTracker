using System.Drawing;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace GCDTracker.Utils {
    public static class RectangleExtensions {
        public static Vector2 LT(this Rectangle rect)  => new(rect.Left, rect.Top);
        public static Vector2 RT(this Rectangle rect)  => new(rect.Right, rect.Top);
        public static Vector2 LB(this Rectangle rect)  => new(rect.Left, rect.Bottom);
        public static Vector2 RB(this Rectangle rect)  => new(rect.Right, rect.Bottom);
    }
}