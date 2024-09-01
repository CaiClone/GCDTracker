using GCDTracker.UI;
using System.Collections.Generic;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using System.Numerics;
using System.Linq;
using System;

namespace GCDTracker {

    public enum EventType {
        FlyOutAlert,
        BarColorPulse,
        BarWidthPulse,
        BarHeightPulse,
        BarBackground,
        WheelPulse,
        FloatingTriangle,
        None
    }

    public enum EventCause {
        Slidecast,
        Queuelock,
        Clipped,
        ABC,
        None
    }

    public class Alert
    {
        public EventType Type { get; set; }
        public EventCause Reason { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float LastClipDelta { get; set; }

        public Alert(EventType type, EventCause reason, float lastClipDelta, float locationX, float locationY)
        {
            Type = type;
            Reason = reason;
            LocationX = locationX;
            LocationY = locationY;
            LastClipDelta = lastClipDelta;
        }
    }

    public class AlertManager
    {
        private static AlertManager instance;
        private readonly Queue<Alert> alertQueue = new Queue<Alert>();
        private AlertManager() { }

        public static AlertManager Instance => instance ??= new AlertManager();

        public int AlertCount => alertQueue.Count;

        public void AddAlert(EventType type, EventCause reason, float locationX = 0f, float locationY = 0f, float lastClipDelta = 0f) {
            alertQueue.Enqueue(new Alert(type, reason, locationX, locationY, lastClipDelta));
        }

        public Alert PeekAlert() => alertQueue.Count > 0 ? alertQueue.Peek() : null;

        public void DequeueAlert() {
            if (alertQueue.Count > 0) {
                alertQueue.Dequeue();
            }
        }

        public void ClearAlerts() => alertQueue.Clear();

        public bool AlertExists(EventType type, EventCause reason) {
            return alertQueue.Any(alert => alert.Type == type && alert.Reason == reason);
        }
    }

    public class GCDEventHandler {
        private static GCDEventHandler instance;

        private const float TransitionDuration = 300f;
        private const float FirstStageEnd = 100f;
        private const float SecondStageEnd = 200f;

        public int PulseWidth { get; private set; }
        public int PulseHeight { get; private set; }
        public Vector4 ProgressPulseColor { get; private set; }
        public float WheelScale { get; private set; }
        private EventCause FlyOutCause;
        private bool BarColor;
        private EventCause BarColorCause;
        private bool BarWidth;
        private EventType BarWidthType;
        private bool BarHeight;
        private EventType BarHeightType;
        private bool wheelPulse;
        private DateTime colorStartTime = DateTime.MinValue;
        private DateTime widthStartTime = DateTime.MinValue;
        private DateTime heightStartTime = DateTime.MinValue;
        private DateTime wheelStartTime = DateTime.MinValue;

        public readonly Easing alertAnimEnabled;
        public readonly Easing alertAnimPos;
        public readonly string[] alertText;

        private GCDEventHandler() {
            alertAnimEnabled = new OutCubic(new(0, 0, 0, 2, 1000)) {
                Point1 = new(0.25f, 0),
                Point2 = new(1f, 0)
            };
            alertAnimPos = new OutCubic(new(0, 0, 0, 1, 500)) {
                Point1 = new(0, 0),
                Point2 = new(0, -20)
            };
            alertText = ["CLIP", "0.0", "0.00", "A-B-C"];
        }

        public static GCDEventHandler Instance => instance ??= new GCDEventHandler();

        public void StartAlert(bool isClip, float ms) {
            if (isClip) {
                alertText[1] = string.Format("{0:0.0}", ms);
                alertText[2] = string.Format("{0:0.00}", ms);
            }
            alertAnimEnabled.Restart();
            alertAnimPos.Restart();
        }

        public void Update(BarInfo bar, Configuration conf, PluginUI ui) {
            int alertCount = AlertManager.Instance.AlertCount;

            for (int i = 0; i < alertCount; i++) {
                var alert = AlertManager.Instance.PeekAlert();
                if (alert == null) continue;
                HandleAlert(alert);
                AlertManager.Instance.DequeueAlert();
            }

            if (bar != null) {    
                UpdateBarProperties(bar, conf);
                UpdateFlyOut(ui, conf, FlyOutCause, (conf.BarWidthRatio + 1) / 2.1f, -0.3f);
            }
            else {
                UpdateWheelProperties(ui.Scale);
                UpdateFlyOut(ui, conf, FlyOutCause, 0.5f, 0f);
            }
        }

        private void HandleAlert(Alert alert) {
            switch (alert.Type) {
                case EventType.FlyOutAlert:
                    FlyOutCause = alert.Reason;
                    FlyOutAlert(alert.LastClipDelta);
                    break;

                case EventType.BarColorPulse when !BarColor:
                    BarColor = true;
                    BarColorCause = alert.Reason;
                    break;

                case EventType.BarWidthPulse when !BarWidth:
                    BarWidth = true;
                    BarWidthType = alert.Type;
                    break;

                case EventType.BarHeightPulse when !BarHeight:
                    BarHeight = true;
                    BarHeightType = alert.Type;
                    break;

                case EventType.WheelPulse when !wheelPulse:
                    wheelPulse = true;
                    break;
            }
        }

        private void UpdateBarProperties(BarInfo bar, Configuration conf) {
            ProgressPulseColor = GetBarColor(conf.frontCol, conf.slideCol, BarColorCause, BarColor);
            PulseWidth = GetBarSize(bar.Width, BarWidthType, BarWidth, ref widthStartTime);
            PulseHeight = GetBarSize(bar.Height, BarHeightType, BarHeight, ref heightStartTime);
        }

        private void UpdateWheelProperties(float uiScale) {
            WheelScale = GetWheelScale(uiScale, wheelPulse);
        }

        private void UpdateFlyOut(PluginUI ui, Configuration conf, EventCause reason, float relx, float rely) {
            switch (reason) {
                case EventCause.Clipped:
                    ui.DrawAlert(relx, rely, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
                    break;
                
                case EventCause.ABC:
                    ui.DrawAlert(relx, rely, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, 3);
                    break;
            }
        }

        private void FlyOutAlert(float lastClipDelta) {
            switch (FlyOutCause) {
                case EventCause.Clipped:
                    StartAlert(true, lastClipDelta);
                    break;
                case EventCause.ABC:
                    StartAlert(false, lastClipDelta);
                    break;
            }
        }

        private Vector4 GetBarColor(Vector4 progressBarColor, Vector4 slideCol, EventCause reason, bool enable) {
            Vector4 targetColor = reason switch {
                EventCause.Queuelock => CalculateTargetColor(progressBarColor),
                EventCause.Slidecast => new Vector4(slideCol.X, slideCol.Y, slideCol.Z, progressBarColor.W),
                _ => progressBarColor
            };

            if (colorStartTime == DateTime.MinValue && enable) {
                colorStartTime = DateTime.Now;
            }

            return ApplyColorTransition(progressBarColor, targetColor, enable, ref BarColor, ref BarColorCause, ref colorStartTime);

            static Vector4 CalculateTargetColor(Vector4 color) {
                return (color.X * 0.3f + color.Y * 0.6f + color.Z * 0.2f) > 0.7f 
                    ? new Vector4(0f, 0f, 0f, color.W) 
                    : new Vector4(1f, 1f, 1f, color.W);
            }
        }

        private int GetBarSize(int dimension, EventType type, bool enable, ref DateTime startTime) {
            int offset = type switch {
                EventType.BarWidthPulse => 10,
                EventType.BarHeightPulse => 5,
                _ => 0
            };

            if (startTime == DateTime.MinValue && enable) {
                startTime = DateTime.Now;
            }

            return ApplySizeTransition(dimension, offset, enable, ref type == EventType.BarWidthPulse ? ref BarWidth : ref BarHeight, ref type == EventType.BarWidthPulse ? ref BarWidthType : ref BarHeightType, ref startTime);
        }

        private Vector4 ApplyColorTransition(Vector4 startColor, Vector4 targetColor, bool enable, ref bool transitionActive, ref EventCause transitionCause, ref DateTime startTime) {
            float elapsedTime = (float)(DateTime.Now - startTime).TotalMilliseconds;

            if (!enable || elapsedTime >= TransitionDuration) {
                transitionActive = false;
                transitionCause = EventCause.None;
                startTime = DateTime.MinValue;
                return startColor;
            }

            return elapsedTime switch {
                < FirstStageEnd => Vector4.Lerp(startColor, targetColor, elapsedTime / FirstStageEnd),
                < SecondStageEnd => targetColor,
                _ => Vector4.Lerp(targetColor, startColor, (elapsedTime - SecondStageEnd) / (TransitionDuration - SecondStageEnd))
            };
        }

        private int ApplySizeTransition(int startDimension, int offset, bool enable, ref bool transitionActive, ref EventType transitionType, ref DateTime startTime) {
            float elapsedTime = (float)(DateTime.Now - startTime).TotalMilliseconds;

            if (!enable || elapsedTime >= TransitionDuration) {
                transitionActive = false;
                transitionType = EventType.None;
                startTime = DateTime.MinValue;
                return startDimension;
            }

            int targetDimension = startDimension + offset;

            return (int)(elapsedTime switch {
                < FirstStageEnd => Lerp(startDimension, targetDimension, elapsedTime / FirstStageEnd),
                < SecondStageEnd => targetDimension,
                _ => Lerp(targetDimension, startDimension, (elapsedTime - SecondStageEnd) / (TransitionDuration - SecondStageEnd))
            });
        }

        public float GetWheelScale(float uiScale, bool enable) {
            if (enable && wheelStartTime == DateTime.MinValue) {
                wheelStartTime = DateTime.Now;
            }

            float elapsedTime = (float)(DateTime.Now - wheelStartTime).TotalMilliseconds;
            float targetScale = uiScale * 1.6f;

            return elapsedTime switch {
                < FirstStageEnd => Lerp(uiScale, targetScale, elapsedTime / FirstStageEnd),
                < SecondStageEnd => targetScale,
                < TransitionDuration => Lerp(targetScale, uiScale, (elapsedTime - SecondStageEnd) / (TransitionDuration - SecondStageEnd)),
                _ => ResetWheel(uiScale)
            };
        }

        private float ResetWheel(float uiScale) {
            wheelPulse = false;
            wheelStartTime = DateTime.MinValue;
            return uiScale;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        
    }
}
