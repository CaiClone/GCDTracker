using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;

namespace GCDTracker.UI {
    public unsafe class BarInfo {
        private static BarInfo instance;
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
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
        public int TriangleOffset { get; private set; }
        public bool IsCastBar { get; private set; }
        public bool IsShortCast { get; private set; }
        public bool IsNonAbility { get; private set; }
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
        public int BorderWidth => (int)(Width * BorderWidthPercent);
        public int RightLimit => (int)(EndVertex.X + 1);

        private BarVertices() { }
        public static BarVertices Instance {
            get {
                instance ??= new BarVertices();
                return instance;
            }
        }

        public void Update(BarInfo bar, BarDecisionHelper go, GCDEventHandler notify) {
            Width = notify.PulseWidth;
            HalfWidth = Width % 2 == 0 ? (Width / 2) : (Width / 2) + 1;
            RawHalfWidth = Width / 2;
            Height = notify.PulseHeight;
            HalfHeight = Height % 2 == 0 ? (Height / 2) : (Height / 2) + 1;
            RawHalfHeight = Height / 2;
            BorderWidthPercent = (float)bar.BorderSizeAdj / (float)bar.Width;

            StartVertex = new((int)(bar.CenterX - RawHalfWidth), (int)(bar.CenterY - RawHalfHeight));
            EndVertex = new((int)(bar.CenterX + HalfWidth), (int)(bar.CenterY + HalfHeight));
            ProgressVertex = new((int)(bar.CenterX + ((bar.CurrentPos + BorderWidthPercent) * Width) - HalfWidth), (int)(bar.CenterY + HalfHeight));
        }
    }
    
    public class BarDecisionHelper {
        private static BarDecisionHelper instance;
        public bool Queue_VerticalBar { get; private set; }
        public bool Queue_Triangle { get; private set; }
        public bool SlideStart_VerticalBar { get; private set; }
        public bool SlideEnd_VerticalBar { get; private set; }
        public bool SlideStart_LeftTri { get; private set; }
        public bool SlideStart_RightTri { get; private set; }
        public bool SlideEnd_RightTri { get; private set; }
        public bool Slide_Background { get; private set; }
        public float Queue_Lock_Start { get; private set; }
        public float Slide_Bar_Start { get; private set; }
        public float Slide_Bar_End { get; private set; }
        private readonly Dictionary<string, bool> triggeredAlerts = [];
        private float previousPos = 1f;
        static readonly float epsilon = 0.02f;
        
        private BarDecisionHelper() {
            triggeredAlerts = [];
         }
        public static BarDecisionHelper Instance {
            get {
                instance ??= new BarDecisionHelper();
                return instance;
            }
        }
        public enum BarState {
            GCDOnly,
            ShortCast,
            LongCast,
            NonAbilityCast,
            NoSlideAbility,
            Idle
        }
        public BarState currentState;

        public void Update(BarInfo bar, Configuration conf, GCDHelper helper, ActionType actionType, ObjectKind objectKind) {                
            if (bar.CurrentPos > (epsilon / bar.TotalBarTime) && bar.CurrentPos < previousPos - epsilon) {
                // Reset
                previousPos = 0f;
                ResetBar(conf);

                // Handle Castbar
                if(bar.IsCastBar){
                    Slide_Bar_Start = bar.GCDTime_SlidecastStart;
                    Slide_Bar_End = conf.SlideCastFullBar ? 1f : bar.GCDTotal_SlidecastEnd;
                    if (bar.IsNonAbility) {
                        Queue_Lock_Start = 0f;
                        Queue_VerticalBar = false;
                        Queue_Triangle = false;
                        Slide_Bar_End = 1f;
                        currentState = objectKind switch
                        {
                            ObjectKind.EventObj 
                            or ObjectKind.EventNpc
                            or ObjectKind.Aetheryte => BarState.NoSlideAbility,
                            _ => actionType switch
                            {
                                ActionType.Mount => BarState.NoSlideAbility,
                                _ => BarState.NonAbilityCast,
                            }
                        };
                    }
                    else if (bar.IsShortCast) {
                        Queue_Lock_Start = bar.QueueLockStart;
                        if (Math.Abs(Slide_Bar_End - Queue_Lock_Start) < epsilon)
                            Slide_Bar_End = Queue_Lock_Start;
                        currentState = BarState.ShortCast;
                    }
                    else if (!bar.IsShortCast) {
                        Queue_Lock_Start = bar.QueueLockStart;
                        currentState = BarState.LongCast;
                    }
                }
                // Handle GCDBar
                else if (!bar.IsCastBar && !bar.IsShortCast) {
                    Queue_Lock_Start = bar.QueueLockStart;
                    currentState = BarState.GCDOnly;
                }
            }

            // Idle State
            else if (!helper.IsRunning)
                currentState = BarState.Idle;

            previousPos = Math.Max(previousPos, bar.CurrentPos);
            
            switch (currentState) {
                case BarState.GCDOnly:
                    if (conf.QueueLockEnabled)
                        HandleGCDOnly(bar, conf);
                    break;

                case BarState.NonAbilityCast:
                    if (conf.SlideCastEnabled)
                        HandleNonAbilityCast(bar, conf);
                    break;

                case BarState.NoSlideAbility:
                    if (conf.SlideCastEnabled)
                        HandleMount();
                    break;

                case BarState.ShortCast:
                    if (conf.SlideCastEnabled)
                        HandleCastBarShort(bar, conf);
                    else if (conf.QueueLockEnabled)
                        HandleGCDOnly(bar, conf);
                    break;

                case BarState.LongCast:
                    if (conf.SlideCastEnabled)
                        HandleCastBarLong(bar, conf);
                    else if (conf.QueueLockEnabled)
                        HandleGCDOnly(bar, conf);
                    break;

                default:
                    ResetBar(conf);
                    break;
            }
        }

        private void HandleGCDOnly(BarInfo bar, Configuration conf) {            
            // draw line
            Queue_VerticalBar = true;      

            // draw triangles
            Queue_Triangle = conf.ShowQueuelockTriangles;

            // activate alerts
            BarCheckQueueEvent(bar, conf);

            // move lines
            if (conf.BarQueueLockSlide)
                Queue_Lock_Start = Math.Max(Queue_Lock_Start, bar.CurrentPos);
        }

        private void HandleNonAbilityCast(BarInfo bar, Configuration conf) {
            // draw lines
            SlideStart_VerticalBar = true;
            SlideStart_LeftTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;
            SlideStart_RightTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;

            // move lines
            Slide_Bar_Start = Math.Max(Slide_Bar_Start, bar.CurrentPos);

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;      
        }

        private void HandleMount() {
            Queue_Lock_Start = 0f;
            Queue_VerticalBar = false;
            Queue_Triangle = false;

            Slide_Bar_Start = 0f;
            Slide_Bar_End = 0f;
            SlideStart_VerticalBar = false;
            SlideEnd_VerticalBar = false;
            SlideStart_LeftTri = false;
            SlideStart_RightTri = false;
            SlideEnd_RightTri = false;
            Slide_Background = false;
        }

        private void HandleCastBarShort(BarInfo bar, Configuration conf) {            
            // draw lines
            SlideStart_VerticalBar = true;
            SlideEnd_VerticalBar = !conf.SlideCastFullBar;

            // draw triangles
            SlideStart_LeftTri = conf.ShowSlidecastTriangles;
            SlideStart_RightTri = conf.ShowSlidecastTriangles && conf.SlideCastFullBar;
            SlideEnd_RightTri = conf.ShowSlidecastTriangles && !conf.SlideCastFullBar;

            // invoke Queuelock
            if (conf.QueueLockEnabled)
                HandleGCDOnly(bar, conf);

            // activate alerts
            BarCheckSlideEvent(bar, conf);

            // move lines
            Slide_Bar_Start = Math.Max(Slide_Bar_Start, Math.Min(bar.CurrentPos, Queue_Lock_Start));
            Slide_Bar_End = Math.Max(Slide_Bar_End, Math.Min(bar.CurrentPos, Queue_Lock_Start));

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;
        }

        private void HandleCastBarLong(BarInfo bar, Configuration conf) {          
            // draw line
            SlideStart_VerticalBar = true;
            
            // draw triangles
            SlideStart_LeftTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;
            SlideStart_RightTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;

            // invoke Queuelock
            if (conf.QueueLockEnabled)
                HandleGCDOnly(bar, conf);

            // activate alerts
            BarCheckSlideEvent(bar, conf);

            // move lines
            Slide_Bar_Start = Math.Max(Slide_Bar_Start, bar.CurrentPos);
            Queue_Lock_Start = Math.Max(Queue_Lock_Start, Math.Min(bar.CurrentPos, Slide_Bar_Start));

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;                
        }

        private void ResetBar(Configuration conf) {
            Queue_Lock_Start = (conf.QueueLockEnabled && conf.BarQueueLockWhenIdle)
                ? 0.8f
                : 0f;
            Queue_VerticalBar = conf.QueueLockEnabled && conf.BarQueueLockWhenIdle;
            Queue_Triangle = Queue_VerticalBar && conf.ShowQueuelockTriangles;

            Slide_Bar_Start = 0f;
            Slide_Bar_End = 0f;
            SlideStart_VerticalBar = false;
            SlideEnd_VerticalBar = false;
            SlideStart_LeftTri = false;
            SlideStart_RightTri = false;
            SlideEnd_RightTri = false;
            Slide_Background = false;
            triggeredAlerts.Clear();
        }

        private bool CheckAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            return triggeredAlerts.ContainsKey(key) && triggeredAlerts[key];
        }

        private void MarkAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            triggeredAlerts[key] = true;
        }

        private void BarCheckSlideEvent(BarInfo bar, Configuration conf){
            var notify = AlertManager.Instance;
            if (bar.CurrentPos >= Slide_Bar_Start - 0.025f && bar.CurrentPos > 0.2f) {
                if (conf.SlideCastEnabled) {
                    if (conf.pulseBarColorAtSlide && !CheckAlert(EventType.BarColorPulse, EventCause.Slidecast)) {
                        notify.ActivateAlert(EventType.BarColorPulse, EventCause.Slidecast, EventSource.Bar);
                        MarkAlert(EventType.BarColorPulse, EventCause.Slidecast);
                    }

                    if (conf.pulseBarWidthAtSlide && !CheckAlert(EventType.BarWidthPulse, EventCause.Slidecast)) {
                        notify.ActivateAlert(EventType.BarWidthPulse, EventCause.Slidecast, EventSource.Bar);
                        MarkAlert(EventType.BarWidthPulse, EventCause.Slidecast);
                    }

                    if (conf.pulseBarHeightAtSlide && !CheckAlert(EventType.BarHeightPulse, EventCause.Slidecast)) {
                        notify.ActivateAlert(EventType.BarHeightPulse, EventCause.Slidecast, EventSource.Bar);
                        MarkAlert(EventType.BarHeightPulse, EventCause.Slidecast);
                    }
                }
            }
        }

        private void BarCheckQueueEvent(BarInfo bar, Configuration conf){
            var notify = AlertManager.Instance;
            if (bar.CurrentPos >= Queue_Lock_Start - 0.025f && bar.CurrentPos > 0.2f) {
                if (conf.QueueLockEnabled) {
                    if (conf.pulseBarColorAtQueue && !CheckAlert(EventType.BarColorPulse, EventCause.Queuelock)) {
                        notify.ActivateAlert(EventType.BarColorPulse, EventCause.Queuelock, EventSource.Bar);
                        MarkAlert(EventType.BarColorPulse, EventCause.Queuelock);
                    }

                    if (conf.pulseBarWidthAtQueue && !CheckAlert(EventType.BarWidthPulse, EventCause.Queuelock)) {
                        notify.ActivateAlert(EventType.BarWidthPulse, EventCause.Queuelock, EventSource.Bar);
                        MarkAlert(EventType.BarWidthPulse, EventCause.Queuelock);
                    }

                    if (conf.pulseBarHeightAtQueue && !CheckAlert(EventType.BarHeightPulse, EventCause.Queuelock)) {
                        notify.ActivateAlert(EventType.BarHeightPulse, EventCause.Queuelock, EventSource.Bar);
                        MarkAlert(EventType.BarHeightPulse, EventCause.Queuelock);
                    }
                }
            }
        }
    }
}
