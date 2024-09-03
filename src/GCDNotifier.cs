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

    public enum EventSource {
        Bar,
        Wheel,
        None
    }

    public class Alert(EventType type, EventCause reason, EventSource source, float lastClipDelta, float locationX, float locationY, bool active, bool started, DateTime startTime)
    {
        public EventType Type { get; set; } = type;
        public EventCause Reason { get; set; } = reason;
        public EventSource Source { get; set; } = source;
        public float LocationX { get; set; } = locationX;
        public float LocationY { get; set; } = locationY;
        public float LastClipDelta { get; set; } = lastClipDelta;
        public bool Active { get; set; } = active;
        public bool Started { get; set; } = started;
        public DateTime StartTime { get; set; } = startTime;
    }

    public class AlertManager
    {
        private static AlertManager instance;
        private readonly Queue<Alert> alertQueue = new Queue<Alert>();
        private AlertManager() {
            InitializeAlerts();
        }
        public static AlertManager Instance => instance ??= new AlertManager();

        private void InitializeAlerts() {
            /// probably should make this not initalize the world ///
            /// I tried a bunch of ways to add the entries when we PeekAlert ///
            /// but for whatever reason, that causes UpdateBarValues to only ///
            /// see one of the two entries (either Slidecast or Queuelock gets ///
            /// ignored).  No idea why that happens and no time to investigate ///
            foreach (EventType type in Enum.GetValues(typeof(EventType))) {
                foreach (EventCause cause in Enum.GetValues(typeof(EventCause))) {
                    foreach (EventSource source in Enum.GetValues(typeof(EventSource))) {
                        if (type != EventType.None && cause != EventCause.None && source != EventSource.None) {
                            alertQueue.Enqueue(new Alert(type, cause, source, 0f, 0f, 0f, false, false, DateTime.MinValue));
                        }
                    }
                }
            }
        }


        public IEnumerable<Alert> PeekAlert(EventType? type = null, EventCause? reason = null, EventSource? source = null) {
            return alertQueue.Where(alert =>
                (!type.HasValue || alert.Type == type.Value) &&
                (!reason.HasValue || alert.Reason == reason.Value) &&
                (!source.HasValue || alert.Source == source.Value));
        }
        public bool AlertActive(EventType type, EventCause reason) {
            return alertQueue.Any(alert => alert.Type == type && alert.Reason == reason && alert.Active);
        }

        public void ActivateAlert(EventType? type = null, EventCause? reason = null, EventSource? source = null, float? lastClipDelta = null) {
            /// this is full of jank and needs refactored ///
            var alertsToKeep = new Queue<Alert>();
            var matchingAlerts = PeekAlert(type, reason, source).ToList();

            while (alertQueue.Count > 0) {
                var alert = alertQueue.Dequeue();
                if (!matchingAlerts.Contains(alert)) {
                    alertsToKeep.Enqueue(alert);
                }
            }

            foreach (var oldAlert in matchingAlerts) {
                var newAlert = new Alert(
                    type: oldAlert.Type,
                    reason: oldAlert.Reason,
                    source: oldAlert.Source,
                    lastClipDelta: lastClipDelta ?? oldAlert.LastClipDelta,
                    locationX: oldAlert.LocationX,
                    locationY: oldAlert.LocationY,
                    active: true,
                    started: oldAlert.Started,
                    startTime: oldAlert.StartTime != DateTime.MinValue ? oldAlert.StartTime : DateTime.Now
                );
                alertsToKeep.Enqueue(newAlert);
            }

            foreach (var alert in alertsToKeep) {
                alertQueue.Enqueue(alert);
            }
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
        public readonly Easing abcAnimEnabled;
        public readonly Easing abcAnimPos;
        public readonly Easing clipAnimEnabled;
        public readonly Easing clipAnimPos;
        public readonly string[] alertText;

        private GCDEventHandler() {
            abcAnimEnabled = new OutCubic(new(0, 0, 0, 2, 1000)) {
                Point1 = new(0.25f, 0),
                Point2 = new(1f, 0)
            };
            abcAnimPos = new OutCubic(new(0, 0, 0, 1, 500)) {
                Point1 = new(0, 0),
                Point2 = new(0, -20)
            };
            clipAnimEnabled = new OutCubic(new(0, 0, 0, 2, 1000)) {
                Point1 = new(0.25f, 0),
                Point2 = new(1f, 0)
            };
            clipAnimPos = new OutCubic(new(0, 0, 0, 1, 500)) {
                Point1 = new(0, 0),
                Point2 = new(0, -20)
            };
            alertText = ["CLIP", "0.0", "0.00", "A-B-C"];
        }

        public static GCDEventHandler Instance => instance ??= new GCDEventHandler();

        public void StartAlert(float ms, EventCause cause) {
            if (cause == EventCause.ABC){
                abcAnimEnabled.Restart();
                abcAnimPos.Restart();
            }
            if (cause == EventCause.Clipped) {
                alertText[1] = string.Format("{0:0.0}", ms);
                alertText[2] = string.Format("{0:0.00}", ms);
                clipAnimEnabled.Restart();
                clipAnimPos.Restart();
            }
        }

        public void Update(BarInfo bar, Configuration conf, PluginUI ui) {
            if (bar != null) {    
                UpdateBarProperties(bar, conf);
                UpdateFlyOutAlerts(ui, conf, EventSource.Bar);
            }
            else {
                UpdateWheelProperties(conf, ui.Scale);
                UpdateFlyOutAlerts(ui, conf, EventSource.Wheel);
            }
        }

        private void UpdateBarProperties(BarInfo bar, Configuration conf) {
            var colorAlerts = AlertManager.Instance.PeekAlert(EventType.BarColorPulse, null, EventSource.Bar);
            foreach (var alert in colorAlerts) {
                ProgressPulseColor = GetBarColor(conf.frontCol, conf.slideCol, alert, conf.subtlePulses);
            }

            var widthAlerts = AlertManager.Instance.PeekAlert(EventType.BarWidthPulse, null, EventSource.Bar);
            foreach (var alert in widthAlerts) {
                PulseWidth = GetBarSize(bar.Width, alert, conf.subtlePulses);
            }

            var heightAlerts = AlertManager.Instance.PeekAlert(EventType.BarHeightPulse, null, EventSource.Bar);
            foreach (var alert in heightAlerts) {
                PulseHeight = GetBarSize(bar.Height, alert, conf.subtlePulses);
            }
        }

        private void UpdateWheelProperties(Configuration conf, float uiScale) {
            var alerts = AlertManager.Instance.PeekAlert(EventType.WheelPulse, EventCause.Queuelock, EventSource.Wheel);
            foreach (var alert in alerts) {
                WheelScale = GetWheelScale(uiScale, alert, conf.subtlePulses);
            }
        }

        public void UpdateFlyOutAlerts(PluginUI ui, Configuration conf, EventSource source) {
            var flyOutAlerts = AlertManager.Instance.PeekAlert(EventType.FlyOutAlert, null, EventSource.Bar);

            foreach (var alert in flyOutAlerts) {
                if (!alert.Active && !alert.Started)
                    continue;

                float relx = (source == EventSource.Bar) ? (conf.BarWidthRatio + 1) / 2.1f : 0.5f;
                float rely = (source == EventSource.Bar) ? -0.3f : 0f;

                if (alert.Active) {
                    StartAlert(alert.LastClipDelta, alert.Reason);
                    alert.Active = false;
                    alert.Started = true;
                }

                ui.DrawAlert(relx, rely, alert);

                switch (alert.Reason) {
                    case EventCause.ABC:
                        if (abcAnimEnabled.IsDone)
                            alert.Started = false;
                        break;

                    case EventCause.Clipped:
                        if (clipAnimEnabled.IsDone)
                            alert.Started = false;
                        break;
                }
            }
        }


        private static Vector4 GetBarColor(Vector4 progressBarColor, Vector4 slideCol, Alert alert, bool subtlePulses) {
            Vector4 targetColor = alert.Reason switch {
                EventCause.Queuelock => CalculateTargetColor(progressBarColor),
                EventCause.Slidecast => new Vector4(slideCol.X, slideCol.Y, slideCol.Z, progressBarColor.W),
                _ => progressBarColor
            };

            if (subtlePulses) {
                targetColor = Vector4.Lerp(targetColor, progressBarColor, 0.5f);
            }

            if (alert.StartTime == DateTime.MinValue && alert.Active) {
                alert.StartTime = DateTime.Now;
            }

            return ApplyColorTransition(progressBarColor, targetColor, alert);

            static Vector4 CalculateTargetColor(Vector4 color) {
                return (color.X * 0.3f + color.Y * 0.6f + color.Z * 0.2f) > 0.7f 
                    ? new Vector4(0f, 0f, 0f, color.W) 
                    : new Vector4(1f, 1f, 1f, color.W);
            }
        }

        private static int GetBarSize(int dimension, Alert alert, bool subtlePulses) {
            int offset = alert.Type switch {
                EventType.BarWidthPulse => subtlePulses ? 5: 10,
                EventType.BarHeightPulse => subtlePulses ? 3: 5,
                _ => 0
            };

            if (alert.StartTime == DateTime.MinValue && alert.Active) {
                alert.StartTime = DateTime.Now;
            }

            return ApplySizeTransition(dimension, offset, alert);
        }

        private static Vector4 ApplyColorTransition(Vector4 startColor, Vector4 targetColor, Alert alert) {
            float elapsedTime = (float)(DateTime.Now - alert.StartTime).TotalMilliseconds;

            if (!alert.Active || elapsedTime >= TransitionDuration) {
                alert.Active = false;
                alert.StartTime = DateTime.MinValue;
                return startColor;
            }

            return elapsedTime switch {
                < FirstStageEnd => Vector4.Lerp(startColor, targetColor, elapsedTime / FirstStageEnd),
                < SecondStageEnd => targetColor,
                _ => Vector4.Lerp(targetColor, startColor, (elapsedTime - SecondStageEnd) / (TransitionDuration - SecondStageEnd))
            };
        }

        private static int ApplySizeTransition(int startDimension, int offset, Alert alert) {
            float elapsedTime = (float)(DateTime.Now - alert.StartTime).TotalMilliseconds;

            if (!alert.Active || elapsedTime >= TransitionDuration) {
                alert.Active = false;
                alert.StartTime = DateTime.MinValue;
                return startDimension;
            }

            int targetDimension = startDimension + offset;

            return (int)(elapsedTime switch {
                < FirstStageEnd => Lerp(startDimension, targetDimension, elapsedTime / FirstStageEnd),
                < SecondStageEnd => targetDimension,
                _ => Lerp(targetDimension, startDimension, (elapsedTime - SecondStageEnd) / (TransitionDuration - SecondStageEnd))
            });
        }

        public static float GetWheelScale(float uiScale, Alert alert, bool subtlePulses) {
            if (alert.Active && alert.StartTime == DateTime.MinValue) {
                alert.StartTime = DateTime.Now;
            }

            float elapsedTime = (float)(DateTime.Now - alert.StartTime).TotalMilliseconds;
            float targetScale = uiScale * (subtlePulses ? 1.3f : 1.6f);

            return elapsedTime switch {
                < FirstStageEnd => Lerp(uiScale, targetScale, elapsedTime / FirstStageEnd),
                < SecondStageEnd => targetScale,
                < TransitionDuration => Lerp(targetScale, uiScale, (elapsedTime - SecondStageEnd) / (TransitionDuration - SecondStageEnd)),
                _ => ResetWheel(uiScale, alert)
            };
        }

        private static float ResetWheel(float uiScale, Alert alert) {
            alert.Active = false;
            alert.StartTime = DateTime.MinValue;
            return uiScale;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        
    }
}
