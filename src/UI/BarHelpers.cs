using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.UI {
    public enum BarState {
        GCDOnly,
        ShortCast,
        LongCast,
        NonAbilityCast,
        NoSlideAbility,
        Idle
    }
    public class BarVertices(Configuration conf) {
        private readonly Configuration conf = conf;
        public Rectangle Rect { get; private set; }
        public int Width {get; private set; }
        public int Height {get; private set; }
        public int HalfBorderSize {get; private set; }

        public int RightLimit => Rect.Right + 1;
        public int BorderSize => conf.BarBorderSizeInt;

        public void Update(PluginUI ui, GCDEventHandler notify) {
            Width = MakeEven((int)(ui.w_size.X * conf.BarWidthRatio));
            Height = MakeEven((int)(ui.w_size.Y * conf.BarHeightRatio));
            //TODO: renable notification change size
            //Width = notify.PulseWidth);
            //Height = MakeEven(notify.PulseHeight);
            Rect = new Rectangle(
                (int)(ui.w_cent.X - (Width / 2)),
                (int)(ui.w_cent.Y - (Height / 2)),
                Width,
                Height
            );
            HalfBorderSize = (BorderSize + 1) / 2;
        }
        public int ProgToScreen(float progress) => (int)(Rect.Left + (progress * Width));
        private static int MakeEven(int value) => value % 2 == 0 ? value : value + 1;
    }
    public unsafe class BarDecisionHelper(Configuration conf) {
        private readonly Configuration conf = conf;
        private readonly Dictionary<string, bool> triggeredAlerts = [];
        private float previousPos = 1f;
        static readonly float epsilon = 0.02f;

        public float CurrentPos { get; internal set; }
        public float TotalBarTime { get; private set; }

        public bool IsCastBar { get; private set; }
        public bool IsShortCast { get; private set; }
        public bool IsNonAbility { get; private set; }

        public bool ShortCastFinished => IsShortCast;

        public BarState CurrentState;
        public float GCDTotal => DataStore.Action->TotalGCD;
        public float CastTotal => DataStore.Action->TotalCastTime;
        public float BarEnd => Math.Max(GCDTotal, CastTotal);

        public System.Action OnReset = delegate { };
        public void Update(GCDHelper helper, ActionType actionType, ObjectKind objectKind) {
            UpdateProgress(helper);
            if (CurrentPos > (epsilon / TotalBarTime) && CurrentPos < previousPos - epsilon) {
                CurrentState = CheckActiveState(actionType, objectKind);
                ResetBar();
            } else if (!helper.IsRunning) {
                CurrentState = BarState.Idle;
                IsShortCast = false;
            }
            previousPos = Math.Max(previousPos, CurrentPos);
        }

        private void UpdateProgress(GCDHelper helper) {
            IsCastBar = conf.CastBarEnabled && GameState.IsCasting();
            if (IsCastBar) {
                float gcdTotal = DataStore.Action->TotalGCD;
                float castTotal = DataStore.Action->TotalCastTime;
                float castElapsed = DataStore.Action->ElapsedCastTime;
                float castbarProgress = castElapsed / castTotal;
                float castbarEnd = 1f;
                bool isTeleport = GameState.IsCastingTeleport();

                // handle short casts
                if (gcdTotal > castTotal) {
                    castbarEnd = GameState.CastingNonAbility() ? 1f : castTotal / gcdTotal;
                }

                IsShortCast = gcdTotal > castTotal;
                IsNonAbility = GameState.CastingNonAbility() || isTeleport || gcdTotal < 0.01f;
                CurrentPos = castbarProgress * castbarEnd;
                TotalBarTime = castbarEnd;
            } else {
                float gcdTotal = helper.TotalGCD;
                float gcdTime = helper.lastElapsedGCD;

                //When cancel casting there is a frame where gcdTime still shows castTime, so check if previous frame was longCast
                if (CurrentState == BarState.LongCast)
                    gcdTime = 0;

                IsNonAbility = false;
                CurrentPos = gcdTime / gcdTotal;
                TotalBarTime = gcdTotal;
            }
        }

        private BarState CheckActiveState(ActionType actionType, ObjectKind objectKind) {
            if(IsCastBar){
                if (IsNonAbility) {
                    return objectKind switch {
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
                    return IsShortCast ? BarState.ShortCast : BarState.LongCast;
                }
            } else if (!IsCastBar && !IsShortCast) {
                return BarState.GCDOnly;
            }
            return BarState.Idle;
        }

        private void ResetBar() {
            previousPos = 0f;
            IsShortCast = false;
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
