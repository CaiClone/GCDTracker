using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.UI {
    public unsafe class BarInfo {
        public float CenterX { get; private set; }
        public float CenterY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BorderSize { get; private set; }
        public int HalfBorderSize { get; private set; }
        public int BorderSizeAdj { get; private set; }
        public float CurrentPos { get; internal set; }
        public float TotalBarTime { get; private set; }

        public bool IsCastBar { get; private set; }
        public bool IsShortCast { get; private set; }
        public bool IsNonAbility { get; private set; }
        public Vector4 ProgressBarColor { get; private set; }

        public void Update(
            Configuration conf,
            float sizeX,
            float centX,
            float sizeY,
            float centY,
            float castBarCurrentPos,
            float totalBarTime,
            bool isCastBar,
            bool isShortCast,
            bool isNonAbility) {
            IsCastBar = isCastBar;
            IsShortCast = isShortCast;
            IsNonAbility = isNonAbility;
            CurrentPos = castBarCurrentPos;
            TotalBarTime = totalBarTime;
            CenterX = centX;
            CenterY = centY;
            Width = (int)(sizeX * conf.BarWidthRatio);
            Height = (int)(sizeY * conf.BarHeightRatio);
            BorderSize = conf.BarBorderSizeInt;
            HalfBorderSize = (BorderSize + 1) / 2;
            BorderSizeAdj = BorderSize >= 1 ? BorderSize : 1;
            ProgressBarColor = conf.frontCol;
        }
    }

    public class BarVertices {
        public Rectangle Rect { get; private set; }
        public int Width {get; private set; }
        public int Height {get; private set; }
        public int BorderWidth {get; private set; }
        public int RightLimit => Rect.Right + 1;

        public void Update(BarInfo bar, BarDecisionHelper go, GCDEventHandler notify) {
            Width = MakeEven(notify.PulseWidth);
            Height = MakeEven(notify.PulseHeight);
            Rect = new Rectangle(
                (int)(bar.CenterX - (Width / 2)),
                (int)(bar.CenterY - (Height / 2)),
                Width,
                Height
            );

            BorderWidth = bar.BorderSizeAdj;
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
    public unsafe class BarDecisionHelper {
        private readonly Dictionary<string, bool> triggeredAlerts = [];
        private float previousPos = 1f;
        static readonly float epsilon = 0.02f;

        public BarState CurrentState;
        public float GCDTotal => DataStore.Action->TotalGCD;
        public float CastTotal => DataStore.Action->TotalCastTime;
        public float BarEnd => Math.Max(GCDTotal, CastTotal);

        public System.Action OnReset = delegate { };
        public void Update(BarInfo bar, GCDHelper helper, ActionType actionType, ObjectKind objectKind) {
            if (bar.CurrentPos > (epsilon / bar.TotalBarTime) && bar.CurrentPos < previousPos - epsilon) {
                // Reset
                previousPos = 0f;
                ResetBar();
                // Handle Castbar
                if(bar.IsCastBar){
                    if (bar.IsNonAbility) {
                        CurrentState = objectKind switch {
                            ObjectKind.EventObj 
                            or ObjectKind.EventNpc
                            or ObjectKind.Aetheryte => BarState.NoSlideAbility,
                            _ => actionType switch
                            {
                                ActionType.Mount => BarState.NoSlideAbility,
                                _ => BarState.NonAbilityCast,
                            }
                        };
                    } else {
                        CurrentState = bar.IsShortCast ? BarState.ShortCast : BarState.LongCast;
                    }
                } else if (!bar.IsCastBar && !bar.IsShortCast) {
                    CurrentState = BarState.GCDOnly;
                }
            } else if (!helper.IsRunning)
                CurrentState = BarState.Idle;
            previousPos = Math.Max(previousPos, bar.CurrentPos);
        }

        private void ResetBar() {
            triggeredAlerts.Clear();
            OnReset?.Invoke();
        }

        public void ActivateAlertIfNeeded(EventType type, bool cond, EventCause cause) {    
            if (cond && !CheckAlert(type, cause)) {
                AlertManager.Instance.ActivateAlert(type, cause, EventSource.Bar);
                MarkAlert(type, cause);
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
    }
}
