using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using GCDTracker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections;

namespace GCDTracker {
    public class GCDEventHandler {
        private readonly Configuration conf;
        private readonly GCDHelper helper;
        public float WheelEventTime;
        public bool WheelScale;
        public float BarEventTime;
        public EventCause BarEventCause;
        public bool BarColor;
        public bool BarWidth;
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

        public GCDEventHandler(Configuration conf) {
            this.conf = conf;
        }

        public void Now(
            EventType type,
            EventCause reason,
            PluginUI ui,
            float locationX = 0f,
            float locationY = 0f,
            float startTime = 0f) {
            switch (type) {
                case EventType.FlyOutAlert:
                    FlyOutAlert(locationX, locationY, reason, ui);
                    break;
                case EventType.BarColorPulse:
                    BarColorPulse(startTime, reason);
                    break;
                case EventType.BarWidthPluse:
                    break;
                case EventType.BarHeightPulse:
                    break;

            }
        }

        private void FlyOutAlert(float relx, float rely, EventCause reason, PluginUI ui){
            switch (reason){
                case EventCause.Clipped:
                    ui.DrawAlert(relx, rely, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
                    break;
                case EventCause.ABC:
                    ui.DrawAlert(relx, rely, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, 3);
                    break;
            }
        }

        private void BarColorPulse(float startTime, EventCause reason){
            BarColor = true;
            BarEventTime = startTime;
            switch (reason){
                case EventCause.Slidecast:
                    BarEventCause = EventCause.Slidecast;
                    break;
                case EventCause.Queuelock:
                    BarEventCause = EventCause.Queuelock;
                    break;
            }
        }
    }
}