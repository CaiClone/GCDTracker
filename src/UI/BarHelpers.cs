using System;
using System.Numerics;
using GCDTracker.Data;
using Lumina.Excel.GeneratedSheets;

namespace GCDTracker.UI {
    public unsafe class BarInfo {
        private static BarInfo instance;
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int PulseWidth { get; private set; }
        public int PulseHeight { get; private set; }
        public int BorderSize { get; private set; }
        public int HalfBorderSize { get; private set; }
        public int BorderSizeAdj { get; private set; }
        public float CurrentPos { get; private set; }
        public float GCDTime_SlidecastStart { get; private set; }
        public float GCDTotal_SlidecastEnd { get; private set; }
        public float TotalBarTime { get; private set; }
        public float GCDTotal { get; private set; }
        public float CastTotal { get; private set; }
        public float QueueLockStart { get; private set; }
        public float QueueLockScaleFactor { get; private set; }
        public float QueueLockScaleFactorCache { get; private set; }
        public int TriangleOffset { get; private set; }
        public bool IsCastBar { get; private set; }
        public bool IsShortCast { get; private set; }
        public bool IsNonAbility { get; private set; }
        public Vector4 ProgressBarColor { get; private set; }
        public Vector4 ProgressPulseColor { get; private set; }

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
            QueueLockScaleFactor = IsCastBar && !isShortCast
                ? GCDTotal / CastTotal
                : 1f;
            QueueLockStart = 0.8f * QueueLockScaleFactor;
            TotalBarTime = totalBarTime;
            CenterX = centX;
            CenterY = centY;
            Width = (int)(sizeX * conf.BarWidthRatio);
            Height = (int)(sizeY * conf.BarHeightRatio);
            BorderSize = conf.BarBorderSizeInt;
            HalfBorderSize = BorderSize % 2 == 0 ? (BorderSize / 2) : (BorderSize / 2) + 1;
            BorderSizeAdj = BorderSize >= 1 ? BorderSize : 1;
            TriangleOffset = triangleOffset;
            ProgressBarColor = conf.frontCol;


            if (conf.pulseBarColorAtQueue || conf.pulseBarWidthAtQueue || conf.pulseBarHeightAtQueue || conf.pulseBarColorAtSlide) {
                if (CurrentPos < 0.02f)
                    QueueLockScaleFactorCache = QueueLockScaleFactor;
                PulseWidth = GetBarSize(
                    Width, 
                    CurrentPos, 
                    QueueLockStart, 
                    GCDTime_SlidecastStart,
                    conf.pulseBarWidthAtQueue, 
                    conf.pulseBarWidthAtSlide,
                    IsCastBar,
                    QueueLockScaleFactorCache);
                PulseHeight = GetBarSize(
                    Height, 
                    CurrentPos, 
                    QueueLockStart, 
                    GCDTime_SlidecastStart,
                    conf.pulseBarHeightAtQueue, 
                    conf.pulseBarHeightAtSlide,
                    IsCastBar,
                    QueueLockScaleFactorCache);
                ProgressPulseColor = GetBarColor(
                    conf.frontCol, 
                    CurrentPos, 
                    QueueLockStart,
                    GCDTime_SlidecastStart,
                    conf.pulseBarColorAtQueue,
                    conf.pulseBarColorAtSlide,
                    QueueLockScaleFactorCache,
                    conf.slideCol,
                    IsCastBar);
            }
        }

        private Vector4 GetBarColor(
            Vector4 progressBarColor, 
            float currentPos, 
            float queueLockStart, 
            float slidecastStart, 
            bool pulseBarColorAtQueue, 
            bool pulseBarColorAtSlide, 
            float scaleFactor, 
            Vector4 slideCol, 
            bool IsCastbar) {

            Vector4 CalculateTargetColor(Vector4 color) {
                return (color.X * 0.3f + color.Y * 0.6f + color.Z * 0.2f) > 0.7f 
                    ? new Vector4(0f, 0f, 0f, color.W) 
                    : new Vector4(1f, 1f, 1f, color.W);
            }

            Vector4 ApplyColorTransition(Vector4 currentColor, float eventStart, Vector4 targetColor) {
                if (currentPos > eventStart - 0.02f * scaleFactor) {
                    if (currentPos < eventStart + 0.02f * scaleFactor) {
                        float factor = (currentPos - eventStart + 0.02f * scaleFactor) / (0.04f * scaleFactor);
                        return Vector4.Lerp(currentColor, targetColor, factor);
                    } 
                    else if (currentPos < eventStart + 0.06f * scaleFactor) {
                        return targetColor;
                    } 
                    else if (currentPos < eventStart + 0.1f * scaleFactor) {
                        float factor = (currentPos - eventStart - 0.06f * scaleFactor) / (0.04f * scaleFactor);
                        return Vector4.Lerp(targetColor, currentColor, factor);
                    }
                }
                return currentColor;
            }

            Vector4 resultColor = progressBarColor;

            if (pulseBarColorAtQueue) {
                Vector4 queueLockTargetColor = CalculateTargetColor(progressBarColor);
                resultColor = ApplyColorTransition(resultColor, queueLockStart, queueLockTargetColor);
            }

            if (IsCastbar && pulseBarColorAtSlide) {
                Vector4 slidecastTargetColor = new Vector4(slideCol.X, slideCol.Y, slideCol.Z, progressBarColor.W);
                resultColor = ApplyColorTransition(resultColor, slidecastStart, slidecastTargetColor);
            }

            return resultColor;
        }

        private int GetBarSize(
            int dimension, 
            float currentPos, 
            float queueLockStart, 
            float slidecastStart, 
            bool pulseAtQueue, 
            bool pulseAtSlide, 
            bool IsCastbar, 
            float scaleFactor) {

            int CalculateSize(int originalSize, float eventStart, float scaleFactor) {
                int targetDimension = (int)(originalSize * 1.2f);

                if (currentPos < eventStart + 0.02f * scaleFactor) {
                    float factor = (currentPos - eventStart + 0.02f * scaleFactor) / (0.04f * scaleFactor);
                    return (int)Lerp(originalSize, targetDimension, factor);
                } 
                else if (currentPos < eventStart + 0.06f * scaleFactor) {
                    return targetDimension;
                } 
                else if (currentPos < eventStart + 0.1f * scaleFactor) {
                    float factor = (currentPos - eventStart - 0.06f * scaleFactor) / (0.04f * scaleFactor);
                    return (int)Lerp(targetDimension, originalSize, factor);
                } 
                else {
                    return originalSize;
                }
            }

            float Lerp(float a, float b, float t) {
                return a + (b - a) * t;
            }

            int resultSize = dimension;

            if (pulseAtQueue && currentPos > queueLockStart - 0.02f * scaleFactor) {
                resultSize = CalculateSize(dimension, queueLockStart, scaleFactor);
            }

            if (IsCastbar && pulseAtSlide && currentPos > slidecastStart - 0.02f * scaleFactor) {
                resultSize = CalculateSize(resultSize, slidecastStart, scaleFactor);
            }

            return resultSize;
        }

    }

    public class BarVertices {
        private static BarVertices instance;
        public Vector2 StartVertex { get; private set; }
        public Vector2 EndVertex { get; private set; }
        public Vector2 ProgressVertex { get; private set; }

        public int Width {get; private set; }
        public int HalfWidth {get; private set; }
        public int RawHalfWidth {get; private set; }
        public int Height {get; private set; }
        public int HalfHeight {get; private set; }
        public int RawHalfHeight {get; private set; }
        public float BorderWidthPercent { get; private set; } 

        private BarVertices() { }
        public static BarVertices Instance {
            get {
                instance ??= new BarVertices();
                return instance;
            }
        }

        public void Update(BarInfo bar, BarDecisionHelper go) {
            Width = go.Allow_Bar_Pulse ? bar.PulseWidth : bar.Width;
            HalfWidth = Width % 2 == 0 ? (Width / 2) : (Width / 2) + 1;
            RawHalfWidth = Width / 2;
            Height = go.Allow_Bar_Pulse ? bar.PulseHeight : bar.Height;
            HalfHeight = Height % 2 == 0 ? (Height / 2) : (Height / 2) + 1;
            RawHalfHeight = Height / 2;
            BorderWidthPercent = (float)bar.BorderSizeAdj / (float)bar.Width;

            StartVertex = new((int)(bar.CenterX - RawHalfWidth), (int)(bar.CenterY - RawHalfHeight));
            EndVertex = new((int)(bar.CenterX + HalfWidth), (int)(bar.CenterY + HalfHeight));
            ProgressVertex = new((int)(bar.CenterX + ((bar.CurrentPos + BorderWidthPercent) * Width) - HalfWidth), (int)(bar.CenterY + HalfHeight));
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

        public void Update (BarInfo bar, BarVertices bar_v, BarDecisionHelper go) {
            int rightClamp = (int)(bar.CenterX + ((go.Slide_Bar_Start + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth);
                rightClamp += bar.TriangleOffset + 1;
                rightClamp = Math.Min(rightClamp, (int)bar_v.EndVertex.X);
            
            TL_C = new(                    
                (int)(bar.CenterX + (go.Slide_Bar_Start * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY - bar_v.RawHalfHeight)
            );
            
            BL_C = new(
                (int)(bar.CenterX + (go.Slide_Bar_Start * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY + bar_v.HalfHeight)
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
                (int)(bar.CenterX + ((go.Slide_Bar_Start + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY + bar_v.HalfHeight)
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

        public void Update (BarInfo bar, BarVertices bar_v, BarDecisionHelper go) {
            int rightClamp = (int)(bar.CenterX + ((go.Slide_Bar_End + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth);
                rightClamp += bar.TriangleOffset + 1;
                rightClamp = Math.Min(rightClamp, (int)bar_v.EndVertex.X);
            
            TL_C = new(                    
                (int)(bar.CenterX + (go.Slide_Bar_End * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY - bar_v.RawHalfHeight)
            );

            BR_C = new(
                (int)(bar.CenterX + ((go.Slide_Bar_End + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY + bar_v.HalfHeight)
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

        public void Update (BarInfo bar, BarVertices bar_v, BarDecisionHelper go) {
            int rightClamp = (int)(bar.CenterX + ((go.Queue_Lock_Start + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth);
                rightClamp += bar.TriangleOffset + 1;
                rightClamp = Math.Min(rightClamp, (int)bar_v.EndVertex.X);
            
            TL_C = new(                    
                (int)(bar.CenterX + (go.Queue_Lock_Start * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY - bar_v.RawHalfHeight)
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
                (int)(bar.CenterX + ((go.Queue_Lock_Start + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY - bar_v.RawHalfHeight)
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
                (int)(bar.CenterX + ((go.Queue_Lock_Start + bar_v.BorderWidthPercent) * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY + bar_v.HalfHeight)
            );
        }
    }
}
