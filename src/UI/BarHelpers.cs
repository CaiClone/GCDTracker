using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using GCDTracker.Utils;

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
            HalfBorderSize = (BorderSize + 1) / 2;
            BorderSizeAdj = BorderSize >= 1 ? BorderSize : 1;
            TriangleOffset = triangleOffset;
            ProgressBarColor = conf.frontCol;

        }
    }

    public class BarVertices {
        private static BarVertices instance;
        public Vector2 ProgressVertex { get; private set; }

        public Rectangle Rect { get; private set; }
        public int Width {get; private set; }
        public int Height {get; private set; }
        public float BorderWidthPercent { get; private set; } 
        public int BorderWidth => (int)(Width * BorderWidthPercent);
        public int RightLimit => Rect.Right + 1;

        private BarVertices() { }
        public static BarVertices Instance {
            get {
                instance ??= new BarVertices();
                return instance;
            }
        }

        public void Update(BarInfo bar, BarDecisionHelper go, GCDEventHandler notify) {
            Width = MakeEven(notify.PulseWidth);
            Height = MakeEven(notify.PulseHeight);
            Rect = new Rectangle(
                (int)(bar.CenterX - (Width / 2)),
                (int)(bar.CenterY - (Height / 2)),
                Width,
                Height
            );

            BorderWidthPercent = (float)bar.BorderSizeAdj / (float)bar.Width;
            ProgressVertex = new(ProgToScreen(bar.CurrentPos + BorderWidthPercent), Rect.Bottom);
        }
        public int ProgToScreen(float progress) => (int)(Rect.Left + (progress * Width));
        private static int MakeEven(int value) => value % 2 == 0 ? value : value + 1;
    }

    public enum BarState {
        GCDOnly,
        ShortCast,
        LongCast,
        NonAbilityCast,
        NoSlideAbility,
        Idle
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
        private readonly Dictionary<string, bool> triggeredAlerts = [];
        private float previousPos = 1f;
        static readonly float epsilon = 0.02f;
        
        public System.Action OnReset = delegate { };
        
        private BarDecisionHelper() {
            triggeredAlerts = [];
         }
        public static BarDecisionHelper Instance {
            get {
                instance ??= new BarDecisionHelper();
                return instance;
            }
        }
        public BarState CurrentState;

        public void Update(BarInfo bar, Configuration conf, GCDHelper helper, ActionType actionType, ObjectKind objectKind) {                
            if (bar.CurrentPos > (epsilon / bar.TotalBarTime) && bar.CurrentPos < previousPos - epsilon) {
                // Reset
                previousPos = 0f;
                ResetBar(conf);

                // Handle Castbar
                if(bar.IsCastBar){
                    if (bar.IsNonAbility) {
                        Queue_Lock_Start = 0f;
                        Queue_VerticalBar = false;
                        Queue_Triangle = false;
                        CurrentState = objectKind switch
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
                        CurrentState = BarState.ShortCast;
                    }
                    else if (!bar.IsShortCast) {
                        Queue_Lock_Start = bar.QueueLockStart;
                        CurrentState = BarState.LongCast;
                    }
                }
                // Handle GCDBar
                else if (!bar.IsCastBar && !bar.IsShortCast) {
                    Queue_Lock_Start = bar.QueueLockStart;
                    CurrentState = BarState.GCDOnly;
                }
            }

            // Idle State
            else if (!helper.IsRunning)
                CurrentState = BarState.Idle;

            previousPos = Math.Max(previousPos, bar.CurrentPos);
            
            switch (CurrentState) {
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

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;      
        }

        private void HandleMount() {
            Queue_Lock_Start = 0f;
            Queue_VerticalBar = false;
            Queue_Triangle = false;

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

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;                
        }

        private void ResetBar(Configuration conf) {
            Queue_Lock_Start = (conf.QueueLockEnabled && conf.BarQueueLockWhenIdle)
                ? 0.8f
                : 0f;
            Queue_VerticalBar = conf.QueueLockEnabled && conf.BarQueueLockWhenIdle;
            Queue_Triangle = Queue_VerticalBar && conf.ShowQueuelockTriangles;

            SlideStart_VerticalBar = false;
            SlideEnd_VerticalBar = false;
            SlideStart_LeftTri = false;
            SlideStart_RightTri = false;
            SlideEnd_RightTri = false;
            Slide_Background = false;
            triggeredAlerts.Clear();
            OnReset();
        }

        public void ActivateAlertIfNeeded(EventType type, bool cond) {
            if (cond && !CheckAlert(type, EventCause.Slidecast)) {
                AlertManager.Instance.ActivateAlert(type, EventCause.Slidecast, EventSource.Bar);
                MarkAlert(type, EventCause.Slidecast);
            }
        }

        private bool CheckAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            return triggeredAlerts.ContainsKey(key) && triggeredAlerts[key];
        }

        private void MarkAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            triggeredAlerts[key] = true;
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
