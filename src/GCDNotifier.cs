
using GCDTracker.UI;
using System.Collections.Generic;
using System.Numerics;

namespace GCDTracker {

        public enum EventType {
            FlyOutAlert,
            BarColorPulse,
            BarWidthPluse,
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

        public float Time { get; set; }
        public float LocationX { get; set; }
        public float LocationY { get; set; }
        public float ScaleFactor { get; set; }

        public Alert(
            EventType type,
            EventCause reason,
            EventSource source,
            float locationX,
            float locationY,
            float startTime,
            float scaleFactor
        )
        {
            Type = type;
            Reason = reason;
            Source = source;
            LocationX = locationX;
            LocationY = locationY;
            Time = startTime;
            ScaleFactor = scaleFactor;
        }
    }

    public class AlertManager
    {
        private static AlertManager instance;
        private readonly Queue<Alert> alertQueue = new Queue<Alert>();
        private AlertManager() { }

        public static AlertManager Instance {
            get
            {
                if (instance == null) {
                    instance = new AlertManager();
                }
                return instance;
            }
        }
        public int AlertCount => alertQueue.Count;
        public void AddAlert(
                EventType type,
                EventCause reason,
                EventSource source,
                float locationX,
                float locationY,
                float startTime,
                float scaleFactor
            ) {
            alertQueue.Enqueue(new Alert(
                type,
                reason,
                source,
                locationX,
                locationY,
                startTime,
                scaleFactor));
        }

        public Alert PeekAlert() {
            return alertQueue.Count > 0 ? alertQueue.Peek() : null;
        }

        public void DequeueAlert() {
            if (alertQueue.Count > 0) {
                alertQueue.Dequeue();
            }
        }

        public void ClearAlerts() {
            alertQueue.Clear();
        }
    }


    public class GCDEventHandler {
        private static GCDEventHandler instance;

        public int PulseWidth { get; private set; }
        public int PulseHeight { get; private set; }
        public Vector4 ProgressPulseColor { get; private set; }
        public float QueueLockScaleFactorCache { get; private set; }
        public EventCause BarEventCause;
        private bool BarColor;
        private float BarColorTime;
        private float BarColorScale;
        private EventCause BarColorCause;
        private bool BarWidth;
        private float BarWidthTime;
        private float BarWidthScale;
        private EventType BarWidthType;
        private bool BarHeight;
        private float BarHeightTime;
        private float BarHeightScale;
        private EventType BarHeightType;

        private GCDEventHandler() { }
        public static GCDEventHandler Instance {
            get {
                instance ??= new GCDEventHandler();
                return instance;
            }
        }

        public void Update(BarInfo bar, Configuration conf, EventSource source, PluginUI ui){
            int alertCount = AlertManager.Instance.AlertCount;
            Queue<Alert> tempQueue = new Queue<Alert>();

            for (int i = 0; i < alertCount; i++) {
                var alert = AlertManager.Instance.PeekAlert();
                if (alert != null) {
                    if (alert.Source == source) {
                        if (alert.Type == EventType.FlyOutAlert) {
                            FlyOutAlert(conf, alert.LocationX, alert.LocationY, alert.Reason, ui);
                        }
                        if (alert.Type == EventType.BarColorPulse && !BarColor) {
                            BarColor = true;
                            BarColorTime = alert.Time;
                            BarColorCause = alert.Reason;
                            BarColorScale = alert.ScaleFactor;
                        }
                        if (alert.Type == EventType.BarWidthPluse && !BarWidth) {
                            BarWidth = true;
                            BarWidthTime = alert.Time;
                            BarWidthType = alert.Type;
                            BarWidthScale = alert.ScaleFactor;
                        }
                        if (alert.Type == EventType.BarHeightPulse && !BarHeight) {
                            BarHeight = true;
                            BarHeightTime = alert.Time;
                            BarHeightType = alert.Type;
                            BarHeightScale = alert.ScaleFactor;
                        }
                        AlertManager.Instance.DequeueAlert();
                    } else {
                        tempQueue.Enqueue(alert);
                        AlertManager.Instance.DequeueAlert();
                    }
                }
            }

            while (tempQueue.Count > 0) {
                AlertManager.Instance.AddAlert(
                    tempQueue.Peek().Type,
                    tempQueue.Peek().Reason,
                    tempQueue.Peek().Source,
                    tempQueue.Peek().LocationX,
                    tempQueue.Peek().LocationY,
                    tempQueue.Peek().Time,
                    tempQueue.Peek().ScaleFactor
                );
                tempQueue.Dequeue();
            }
            
            if (bar != null) {    
                ProgressPulseColor = GetBarColor(
                    conf.frontCol,
                    conf.slideCol,
                    bar.CurrentPos, 
                    BarColorTime,
                    BarColorScale,
                    BarColorCause);
                PulseWidth = GetBarSize(
                    bar.Width, 
                    bar.CurrentPos,
                    BarHeightTime,
                    BarHeightScale,
                    BarWidthType);
                PulseHeight = GetBarSize(
                    bar.Height, 
                    bar.CurrentPos,
                    BarHeightTime,
                    BarHeightScale,
                    BarHeightType);
                    GCDTracker.Log.Warning(PulseWidth.ToString());
            }
        }

        private void FlyOutAlert(Configuration conf,float relx, float rely, EventCause reason, PluginUI ui){
            switch (reason){
                case EventCause.Clipped:
                    ui.DrawAlert(relx, rely, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
                    break;
                case EventCause.ABC:
                    ui.DrawAlert(relx, rely, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, 3);
                    break;
            }
        }

        private Vector4 GetBarColor(
            Vector4 progressBarColor,
            Vector4 slideCol, 
            float currentPos, 
            float startTime,
            float scaleFactor,
            EventCause reason) {

            Vector4 CalculateTargetColor(Vector4 color) {
                return (color.X * 0.3f + color.Y * 0.6f + color.Z * 0.2f) > 0.7f 
                    ? new Vector4(0f, 0f, 0f, color.W) 
                    : new Vector4(1f, 1f, 1f, color.W);
            }

            Vector4 ApplyColorTransition(Vector4 currentColor, float eventStart, Vector4 targetColor) {
                if (currentPos > eventStart - 0.05f * scaleFactor) {
                    if (currentPos < eventStart + 0.05f * scaleFactor) {
                        float factor = (currentPos - eventStart + 0.05f * scaleFactor) / (0.05f * scaleFactor);
                        return Vector4.Lerp(currentColor, targetColor, factor);
                    } 
                    else if (currentPos < eventStart + 0.1f * scaleFactor) {
                        return targetColor;
                    } 
                    else if (currentPos < eventStart + 0.15f * scaleFactor) {
                        float factor = (currentPos - eventStart - 0.15f * scaleFactor) / (0.05f * scaleFactor);
                        return Vector4.Lerp(targetColor, currentColor, factor);
                    }
                }
                BarColor = false;
                return currentColor;
            }

            Vector4 resultColor = progressBarColor;
            Vector4 targetColor = progressBarColor;
            if (reason == EventCause.Queuelock)
                targetColor = CalculateTargetColor(progressBarColor);
            if (reason == EventCause.Slidecast)
                targetColor = new Vector4(slideCol.X, slideCol.Y, slideCol.Z, progressBarColor.W);
            resultColor = ApplyColorTransition(resultColor, startTime, targetColor);

            return resultColor;
        }

        private int GetBarSize(
            int dimension, 
            float currentPos, 
            float starTime, 
            float scaleFactor,
            EventType type) {

            int CalculateSize(int originalSize, float eventStart, float scaleFactor, int offset) {
                int targetDimension = originalSize + offset;

                if (currentPos < eventStart + 0.05f * scaleFactor) {
                    float factor = (currentPos - eventStart + 0.05f * scaleFactor) / (0.05f * scaleFactor);
                    return (int)Lerp(originalSize, targetDimension, factor);
                } 
                else if (currentPos < eventStart + 0.1f * scaleFactor) {
                    return targetDimension;
                } 
                else if (currentPos < eventStart + 0.15f * scaleFactor) {
                    float factor = (currentPos - eventStart - 0.15f * scaleFactor) / (0.05f * scaleFactor);
                    return (int)Lerp(targetDimension, originalSize, factor);
                } 
                else {
                    if (type == EventType.BarWidthPluse)
                        BarWidth = false;
                    if (type == EventType.BarHeightPulse)
                        BarHeight = false;
                    return originalSize;
                }
            }

            float Lerp(float a, float b, float t) {
                return a + (b - a) * t;
            }

            int offset = 0;
            if (type == EventType.BarWidthPluse)
                offset = 10;
            if (type == EventType.BarHeightPulse)
                offset = 5;
            int resultSize = CalculateSize(dimension, starTime, scaleFactor, offset);
            return resultSize;
        }

    }
}