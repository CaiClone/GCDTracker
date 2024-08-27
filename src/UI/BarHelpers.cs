using System;
using System.Numerics;
using GCDTracker.Data;

namespace GCDTracker.UI {
    public unsafe class BarInfo {
        private static BarInfo instance;
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public int Width { get; private set; }
        public int HalfWidth { get; private set; }
        public int RawHalfWidth { get; private set; }
        public int Height { get; private set; }
        public int HalfHeight { get; private set; }
        public int RawHalfHeight { get; private set; }
        public int BorderSize { get; private set; }
        public int HalfBorderSize { get; private set; }
        public int BorderSizeAdj { get; private set; }
        public float BorderWidthPercent { get; private set; }
        public float CurrentPos { get; private set; }
        public float GCDTime_SlidecastStart { get; private set; }
        public float GCDTotal_SlidecastEnd { get; private set; }
        public float TotalBarTime { get; private set; }
        public float GCDTotal { get; private set; }
        public float CastTotal { get; private set; }
        public float QueueLockStart { get; private set; }
        public float QueueLockScaleFactor { get; private set; }
        public int TriangleOffset { get; private set; }
        public bool IsCastBar { get; private set; }
        public bool IsShortCast { get; private set; }
        public bool IsNonAbility { get; private set; }
        public Vector2 StartVertex { get; private set; }
        public Vector2 EndVertex { get; private set; }
        public Vector2 ProgressVertex { get; private set; }
        public Vector4 ProgressBarColor { get; private set; }

        private BarInfo() { }
        public static BarInfo Instance {
            get {
                instance ??= new BarInfo();
                return instance;
            }
        }

        public void Update(
            Configuration conf,
            float sizeX,
            float centX,
            float sizeY,
            float centY,
            float castBarCurrentPos,
            float gcdTime_slidecastStart,
            float gcdTotal_slidecastEnd,
            float totalBarTime,
            int triangleOffset,
            bool isCastBar,
            bool isShortCast,
            bool isNonAbility) {

            IsCastBar = isCastBar;
            IsShortCast = isShortCast;
            IsNonAbility = isNonAbility;
            CurrentPos = castBarCurrentPos;
            GCDTotal = DataStore.Action->TotalGCD;
            CastTotal = DataStore.Action->TotalCastTime;
            GCDTime_SlidecastStart = gcdTime_slidecastStart;
            GCDTotal_SlidecastEnd = gcdTotal_slidecastEnd;
            QueueLockStart = IsCastBar && !isShortCast
                ? 0.8f * (GCDTotal / CastTotal)
                : 0.8f;
            QueueLockScaleFactor = IsCastBar && !isShortCast
                ? GCDTotal / CastTotal
                : 1f;
            TotalBarTime = totalBarTime;
            CenterX = centX;
            CenterY = centY;
            Width = GetBarSize(sizeX, conf.BarWidthRatio, CurrentPos, QueueLockStart, conf.pulseBarWidthAtQueue, QueueLockScaleFactor);
            HalfWidth = Width % 2 == 0 ? (Width / 2) : (Width / 2) + 1;
            RawHalfWidth = Width / 2;
            Height = GetBarSize(sizeY, conf.BarHeightRatio, CurrentPos, QueueLockStart, conf.pulseBarHeightAtQueue, QueueLockScaleFactor);
            HalfHeight = Height % 2 == 0 ? (Height / 2) : (Height / 2) + 1;
            RawHalfHeight = Height / 2;
            BorderSize = conf.BarBorderSizeInt;
            HalfBorderSize = BorderSize % 2 == 0 ? (BorderSize / 2) : (BorderSize / 2) + 1;
            BorderSizeAdj = BorderSize >= 1 ? BorderSize : 1;
            BorderWidthPercent = (float)BorderSizeAdj / (float)Width;
            TriangleOffset = triangleOffset;
            ProgressBarColor = GetBarColor(conf.frontCol, CurrentPos, QueueLockStart, conf.pulseBarColorAtQueue, QueueLockScaleFactor);

            StartVertex = new(
                (int)(CenterX - RawHalfWidth),
                (int)(CenterY - RawHalfHeight)
            );
            EndVertex = new(
                (int)(CenterX + HalfWidth),
                (int)(CenterY + HalfHeight)
            );
            ProgressVertex = new(
                (int)(CenterX + ((CurrentPos + BorderWidthPercent) * Width) - HalfWidth),
                (int)(CenterY + HalfHeight)
            );
        }

        private Vector4 GetBarColor(Vector4 progressBarColor, float currentPos, float queueLockStart, bool pulseBarColorAtQueue, float scaleFactor) {
            if (currentPos <= queueLockStart - 0.02f * scaleFactor || !pulseBarColorAtQueue)
                return progressBarColor;

            Vector4 targetColor = (progressBarColor.X * 0.3f + progressBarColor.Y * 0.6f + progressBarColor.Z * 0.2f) > 0.7f 
                                ? new Vector4(0f, 0f, 0f, progressBarColor.W) 
                                : new Vector4(1f, 1f, 1f, progressBarColor.W);

            if (currentPos < queueLockStart + 0.02f * scaleFactor) {
                float factor = (currentPos - queueLockStart + 0.02f * scaleFactor) / (0.04f * scaleFactor);
                GCDTracker.Log.Warning(factor.ToString());
                return Vector4.Lerp(progressBarColor, targetColor, factor);
            }
            else if (currentPos < queueLockStart + 0.06f * scaleFactor) {
                GCDTracker.Log.Warning(" ");
                return targetColor;
            }
            else if (currentPos < queueLockStart + 0.1f * scaleFactor) {
                float factor = (currentPos - queueLockStart - 0.06f * scaleFactor) / (0.04f * scaleFactor);
                GCDTracker.Log.Warning(factor.ToString());
                return Vector4.Lerp(targetColor, progressBarColor, factor);
            }
            else {
                return progressBarColor;
            }
        }

        private int GetBarSize(float size, float ratio, float currentPos, float queueLockStart, bool pulseAtQueue, float scaleFactor) {
            int dimension = (int)(size * ratio);
            if (currentPos <= queueLockStart - 0.02f * scaleFactor || !pulseAtQueue)
                return dimension;
            int targetDimension = (int)(dimension * 1.2f);

            if (currentPos < queueLockStart + 0.02f * scaleFactor) {
                float factor = (currentPos - queueLockStart + 0.02f * scaleFactor) / (0.04f * scaleFactor);
                return (int)Lerp(dimension, targetDimension, factor);
            }
            else if (currentPos < queueLockStart + 0.06f * scaleFactor) {
                return targetDimension;
            }
            else if (currentPos < queueLockStart + 0.1f * scaleFactor) {
                float factor = (currentPos - queueLockStart - 0.06f * scaleFactor) / (0.04f * scaleFactor);
                return (int)Lerp(targetDimension, dimension, factor);
            }
            else {
                return dimension;
            }
        }

        private float Lerp(float start, float end, float factor) {
            return start + factor * (end - start);
        }

    }

    public class SlideCastStartVertices {
        private static SlideCastStartVertices instance;
        public Vector2 TL_C { get; private set; }

        public Vector2 BL_C { get; private set; }
        public Vector2 BL_X { get; private set; }
        public Vector2 BL_Y { get; private set; }

        public Vector2 BR_C { get; private set; }
        public Vector2 BR_X { get; private set; }
        public Vector2 BR_Y { get; private set; }

        private SlideCastStartVertices() { }
        public static SlideCastStartVertices Instance {
            get {
                instance ??= new SlideCastStartVertices();
                return instance;
            }
        }

        public void Update (BarInfo bar, BarDecisionHelper go) {
            int rightClamp = (int)(bar.CenterX + ((go.Slide_Bar_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth);
                rightClamp += bar.TriangleOffset + 1;
                rightClamp = Math.Min(rightClamp, (int)bar.EndVertex.X);
            
            TL_C = new(                    
                (int)(bar.CenterX + (go.Slide_Bar_Start * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY - bar.RawHalfHeight)
            );
            
            BL_C = new(
                (int)(bar.CenterX + (go.Slide_Bar_Start * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY + bar.HalfHeight)
            );
            BL_X = new(
                BL_C.X - bar.TriangleOffset,
                BL_C.Y
            );
            BL_Y = new(
                BL_C.X,
                BL_C.Y - bar.TriangleOffset
            );

            BR_C = new(
                (int)(bar.CenterX + ((go.Slide_Bar_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY + bar.HalfHeight)
            );
            BR_X = new(
                rightClamp,
                BR_C.Y
            );
            BR_Y = new(
                BR_C.X,
                BR_C.Y - (bar.TriangleOffset + 1)
            );
        }
    }

    public class SlideCastEndVertices {
        private static SlideCastEndVertices instance;
        public Vector2 TL_C { get; private set;}

        public Vector2 BR_C { get; private set;}
        public Vector2 BR_X { get; private set;}
        public Vector2 BR_Y { get; private set;}

        private SlideCastEndVertices() { }
        public static SlideCastEndVertices Instance {
            get {
                instance ??= new SlideCastEndVertices();
                return instance;
            }
        }

        public void Update (BarInfo bar, BarDecisionHelper go) {
            int rightClamp = (int)(bar.CenterX + ((go.Slide_Bar_End + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth);
                rightClamp += bar.TriangleOffset + 1;
                rightClamp = Math.Min(rightClamp, (int)bar.EndVertex.X);
            
            TL_C = new(                    
                (int)(bar.CenterX + (go.Slide_Bar_End * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY - bar.RawHalfHeight)
            );

            BR_C = new(
                (int)(bar.CenterX + ((go.Slide_Bar_End + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY + bar.HalfHeight)
            );
            BR_X = new(
                rightClamp,
                BR_C.Y
            );
            BR_Y = new(
                BR_C.X,
                BR_C.Y - (bar.TriangleOffset + 1)
            );
        }
    }

    public class QueueLockVertices {
        private static QueueLockVertices instance;
        public Vector2 TL_C { get; private set;}
        public Vector2 TL_X { get; private set;}
        public Vector2 TL_Y { get; private set;}

        public Vector2 TR_C { get; private set;}
        public Vector2 TR_X { get; private set;}
        public Vector2 TR_Y { get; private set;}

        public Vector2 BR_C { get; private set;}

        private QueueLockVertices() { }
        public static QueueLockVertices Instance {
            get {
                instance ??= new QueueLockVertices();
                return instance;
            }
        }

        public void Update (BarInfo bar, BarDecisionHelper go) {
            int rightClamp = (int)(bar.CenterX + ((go.Queue_Lock_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth);
                rightClamp += bar.TriangleOffset + 1;
                rightClamp = Math.Min(rightClamp, (int)bar.EndVertex.X);
            
            TL_C = new(                    
                (int)(bar.CenterX + (go.Queue_Lock_Start * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY - bar.RawHalfHeight)
            );
            TL_X = new(
                TL_C.X - bar.TriangleOffset, 
                TL_C.Y
            );
            TL_Y = new(
                TL_C.X, 
                TL_C.Y + bar.TriangleOffset
            );

            TR_C = new(
                (int)(bar.CenterX + ((go.Queue_Lock_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY - bar.RawHalfHeight)
            );
            TR_X = new(
                rightClamp,
                TR_C.Y
            );
            TR_Y = new(
                TR_C.X,
                TR_C.Y + (bar.TriangleOffset + 1)
            );

            BR_C = new(
                (int)(bar.CenterX + ((go.Queue_Lock_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth),
                (int)(bar.CenterY + bar.HalfHeight)
            );
        }
    }
}
