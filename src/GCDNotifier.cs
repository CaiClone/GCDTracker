using GCDTracker.UI;
using System.Collections.Generic;
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

    public enum EventSource {
        Wheel,
        Bar,
        None
    }

    public class Alert
    {
        public EventType Type { get; set; }
        public EventCause Reason { get; set; }
        public EventSource Source { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }

        public Alert(EventType type, EventCause reason, EventSource source, float locationX, float locationY)
        {
            Type = type;
            Reason = reason;
            Source = source;
            LocationX = locationX;
            LocationY = locationY;
        }
    }

    public class AlertManager
    {
        private static AlertManager instance;
        private readonly Queue<Alert> alertQueue = new Queue<Alert>();
        private AlertManager() { }

        public static AlertManager Instance => instance ??= new AlertManager();

        public int AlertCount => alertQueue.Count;

        public void AddAlert(EventType type, EventCause reason, EventSource source, float locationX, float locationY) {
            alertQueue.Enqueue(new Alert(type, reason, source, locationX, locationY));
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

        public int PulseWidth { get; private set; }
        public int PulseHeight { get; private set; }
        public Vector4 ProgressPulseColor { get; private set; }
        public float WheelScale { get; private set; }
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

        private GCDEventHandler() { }

        public static GCDEventHandler Instance => instance ??= new GCDEventHandler();

        public void Update(BarInfo bar, Configuration conf, EventSource source, PluginUI ui) {
            int alertCount = AlertManager.Instance.AlertCount;
            Queue<Alert> tempQueue = new Queue<Alert>();

            for (int i = 0; i < alertCount; i++) {
                var alert = AlertManager.Instance.PeekAlert();
                if (alert == null) continue;

                if (alert.Source == source) {
                    HandleAlert(alert, conf, ui);
                    AlertManager.Instance.DequeueAlert();
                } else {
                    tempQueue.Enqueue(alert);
                    AlertManager.Instance.DequeueAlert();
                }
            }

            while (tempQueue.Count > 0) {
                var alert = tempQueue.Dequeue();
                AlertManager.Instance.AddAlert(alert.Type, alert.Reason, alert.Source, alert.LocationX, alert.LocationY);
            }
            
            if (bar != null) {    
                UpdateBarProperties(bar, conf);
            }
            if (bar == null) {
                UpdateWheelProperties(ui.Scale);

            }
        }

        private void HandleAlert(Alert alert, Configuration conf, PluginUI ui) {
            switch (alert.Type) {
                case EventType.FlyOutAlert:
                    FlyOutAlert(conf, alert.LocationX, alert.LocationY, alert.Reason, ui);
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

        private static void FlyOutAlert(Configuration conf, float relX, float relY, EventCause reason, PluginUI ui) {
            switch (reason) {
                case EventCause.Clipped:
                    ui.DrawAlert(relX, relY, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
                    break;
                case EventCause.ABC:
                    ui.DrawAlert(relX, relY, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, 3);
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
            const float transitionDuration = 300f;

            if (!enable || elapsedTime >= transitionDuration) {
                transitionActive = false;
                transitionCause = EventCause.None;
                startTime = DateTime.MinValue;
                return startColor;
            }

            const float firstStageEnd = 100f;
            const float secondStageEnd = 200f;

            return elapsedTime switch {
                < firstStageEnd => Vector4.Lerp(startColor, targetColor, elapsedTime / firstStageEnd),
                < secondStageEnd => targetColor,
                _ => Vector4.Lerp(targetColor, startColor, (elapsedTime - secondStageEnd) / (transitionDuration - secondStageEnd))
            };
        }

        private int ApplySizeTransition(int startDimension, int offset, bool enable, ref bool transitionActive, ref EventType transitionType, ref DateTime startTime) {
            float elapsedTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
            const float transitionDuration = 300f;

            if (!enable || elapsedTime >= transitionDuration) {
                transitionActive = false;
                transitionType = EventType.None;
                startTime = DateTime.MinValue;
                return startDimension;
            }

            int targetDimension = startDimension + offset;
            const float firstStageEnd = 100f;
            const float secondStageEnd = 200f;

            return (int)(elapsedTime switch {
                < firstStageEnd => Lerp(startDimension, targetDimension, elapsedTime / firstStageEnd),
                < secondStageEnd => targetDimension,
                _ => Lerp(targetDimension, startDimension, (elapsedTime - secondStageEnd) / (transitionDuration - secondStageEnd))
            });
        }

        public float GetWheelScale(float uiScale, bool enable) {
            const float transitionDuration = 300f;
            const float firstStageEnd = 100f;
            const float secondStageEnd = 200f;

            if (enable && wheelStartTime == DateTime.MinValue) {
                wheelStartTime = DateTime.Now;
            }

            float elapsedTime = (float)(DateTime.Now - wheelStartTime).TotalMilliseconds;
            float targetScale = uiScale * 1.6f;

            return elapsedTime switch
            {
                < firstStageEnd => Lerp(uiScale, targetScale, elapsedTime / firstStageEnd),
                < secondStageEnd => targetScale,
                < transitionDuration => Lerp(targetScale, uiScale, (elapsedTime - secondStageEnd) / (transitionDuration - secondStageEnd)),
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
