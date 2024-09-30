using GCDTracker.UI;
using System.Collections.Generic;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using System.Numerics;
using System.Linq;
using System;
using static GCDTracker.EventType;
using static GCDTracker.EventCause;
using static GCDTracker.EventSource;
using System.Security.Cryptography;
using GCDTracker.Config;

namespace GCDTracker {

    public enum EventType {
        FlyOutAlert,
        BarColorPulse,
        BarWidthPulse,
        BarHeightPulse,
        BarBackground,
        WheelPulse,
        None
    }

    public enum EventCause {
        Slidecast,
        Queuelock,
        Clip,
        ABC,
        None
    }

    public enum EventSource {
        Bar,
        Wheel,
        None
    }

    public class Alert(EventType type, EventCause reason, EventSource source) {
        public EventType Type { get; } = type;
        public EventCause Reason { get; } = reason;
        public EventSource Source { get; } = source;
        public float LastClipDelta { get; set; } = 0f;
        public bool Active { get; set; } = false;
        public bool Started { get; set; } = false;
        public DateTime StartTime { get; set; } = DateTime.MinValue;
    }

    public class AlertManager {
        private static AlertManager instance;
        private readonly Dictionary<(EventType, EventCause, EventSource), Alert> alertDictionary = new();

        private AlertManager() { }

        public static AlertManager Instance => instance ??= new AlertManager();

        private Alert GetOrCreateAlert(EventType type, EventCause cause, EventSource source) {
            var key = (type, cause, source);

            if (!alertDictionary.TryGetValue(key, out var alert)) {
                alert = new Alert(type, cause, source);
                alertDictionary[key] = alert;
            }

            return alert;
        }

        public IEnumerable<Alert> PeekAlert(EventType type, EventCause[] causes, EventSource source) {
            foreach (var cause in causes) {
                var key = (type, cause, source);

                if (alertDictionary.TryGetValue(key, out var alert) && (alert.Active || alert.Started)) {
                    yield return alert;
                }
            }
        }

        public Dictionary<(EventType, EventCause, EventSource), Alert> GetAlertDictionary() => alertDictionary;

        public void ActivateAlert(EventType type, EventCause cause, EventSource source, float? lastClipDelta = null) {
            var alert = GetOrCreateAlert(type, cause, source);

            alert.LastClipDelta = lastClipDelta ?? alert.LastClipDelta;
            alert.Active = true;
            alert.Started = false;
            alert.StartTime = DateTime.Now;
        }
    }

    public class GCDEventHandler {
        private static GCDEventHandler instance;

        private const float TransitionDuration = 300f;
        private const float FirstStageEnd = 100f;
        private const float SecondStageEnd = 200f;

        public int? PulseWidth { get; private set; }
        public int? PulseHeight { get; private set; }
        public Vector4 ProgressPulseColor { get; private set; }
        public float WheelScale { get; private set; }
        public readonly Easing abcAnimEnabled;
        public readonly Easing abcAnimPos;
        public readonly Easing clipAnimEnabled;
        public readonly Easing clipAnimPos;
        public readonly string[] alertText;
        // private DateTime lastLogTime = DateTime.MinValue;

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
            alertText = new[] { "CLIP", "0.0", "0.00", "A-B-C" };
        }

        public static GCDEventHandler Instance => instance ??= new GCDEventHandler();

        public void StartAlert(float ms, EventCause cause) {
            if (cause == EventCause.ABC){
                abcAnimEnabled.Restart();
                abcAnimPos.Restart();
            }
            if (cause == EventCause.Clip) {
                alertText[1] = $"{ms:0.0}";
                alertText[2] = $"{ms:0.00}";
                clipAnimEnabled.Restart();
                clipAnimPos.Restart();
            }
        }

        /*
        private static void LogAlertManagerContents() {
            var alertManager = AlertManager.Instance;
            GCDTracker.Log.Warning(new string('=', 10));
            foreach (var kvp in alertManager.GetAlertDictionary()) {
                var key = kvp.Key;
                var alert = kvp.Value;
                GCDTracker.Log.Warning($"Alert - Type: {key.Item1}, Cause: {key.Item2}, Source: {key.Item3}, " +
                                    $"Active: {alert.Active}, Started: {alert.Started}, LastClipDelta: {alert.LastClipDelta}, " +
                                    $"StartTime: {alert.StartTime}");
            }
        }
        */

        public void Update(BarVertices bar_v, Configuration conf, PluginUI ui) {
            if (bar_v != null) {    
                UpdateBarProperties(bar_v, conf);
                UpdateFlyOutAlerts(ui, conf, EventSource.Bar);
            }
            else {
                UpdateWheelProperties(conf, ui.Scale);
                UpdateFlyOutAlerts(ui, conf, EventSource.Wheel);
            }
            /*
            if ((DateTime.Now - lastLogTime).TotalSeconds >= 1) {
                LogAlertManagerContents();
                lastLogTime = DateTime.Now;
            }
            */
        }

        private static IEnumerable<Alert> PeekAlert(EventType type, EventCause[] causes, EventSource source) {
            return AlertManager.Instance.PeekAlert(type, causes, source);
        }

        private void UpdateBarProperties(BarVertices bar_v, Configuration conf) {
            var causes = new[] { Slidecast, Queuelock };

            var colorAlert = PeekAlert(BarColorPulse, causes, Bar).FirstOrDefault();
            ProgressPulseColor = colorAlert != null 
                ? GetBarColor(conf, colorAlert) 
                : conf.frontCol;

            var widthAlert = PeekAlert(BarWidthPulse, causes, Bar).FirstOrDefault();
            PulseWidth = widthAlert != null 
                ? GetBarSize(bar_v.BaseWidth, widthAlert, conf.subtlePulses) 
                : null;

            var heightAlert = PeekAlert(BarHeightPulse, causes, Bar).FirstOrDefault();
            PulseHeight = heightAlert != null 
                ? GetBarSize(bar_v.BaseHeight, heightAlert, conf.subtlePulses) 
                : null;
        }


        private void UpdateWheelProperties(Configuration conf, float uiScale) {
            var causes = new[] { Queuelock };

            var scaleAlert = PeekAlert(WheelPulse, causes, Wheel).FirstOrDefault();
            WheelScale = scaleAlert != null 
                ? GetWheelScale(uiScale, scaleAlert, conf.subtlePulses) 
                : uiScale;
        }

        private void UpdateFlyOutAlerts(PluginUI ui, Configuration conf, EventSource source) {
            var causes = new[] { ABC, Clip };
            var flyOutAlerts = PeekAlert(FlyOutAlert, causes, source);
            foreach (var alert in flyOutAlerts) {
                if (!alert.Active && !alert.Started)
                    continue;

                float relx = (source == Bar) ? (conf.BarWidthRatio + 1) / 2.1f : 0.5f;
                float rely = (source == Bar) ? -0.3f : 0f;

                if (alert.Active) {
                    StartAlert(alert.LastClipDelta, alert.Reason);
                    alert.Active = false;
                    alert.Started = true;
                }

                ui.DrawAlert(relx, rely, alert);

                if (alert.Reason == ABC && abcAnimEnabled.IsDone || alert.Reason == Clip && clipAnimEnabled.IsDone) {
                    alert.Started = false;
                }
            }
        }

        private static Vector4 GetBarColor(Configuration conf, Alert alert) {
            Vector4 targetColor = alert.Reason switch {
                EventCause.Queuelock => new Vector4(conf.QueuePulseCol.X, conf.QueuePulseCol.Y, conf.QueuePulseCol.Z, conf.frontCol.W),
                EventCause.Slidecast => new Vector4(conf.slideCol.X, conf.slideCol.Y, conf.slideCol.Z, conf.frontCol.W),
                _ => conf.frontCol
            };

            if (conf.subtlePulses) {
                targetColor = Vector4.Lerp(targetColor, conf.frontCol, 0.5f);
            }

            if (alert.StartTime == DateTime.MinValue && alert.Active) {
                alert.StartTime = DateTime.Now;
            }

            return ApplyColorTransition(conf.frontCol, targetColor, alert);
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

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
