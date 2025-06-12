using System;
using System.Numerics;
using GCDTracker.Data;
using GCDTracker.UI.Components;

namespace GCDTracker.UI {
    public unsafe class FloatingAlerts(Configuration conf, GCDHelper helper, GCDBar gcdBar) : IWindow {
        protected readonly Configuration conf = conf;
        protected readonly GCDHelper helper = helper;
        protected readonly GCDBar bar = gcdBar;

        public void Draw(PluginUI ui) {
            float gcdTotal = DataStore.Action->TotalGCD;
            float gcdElapsed = DataStore.Action->ElapsedGCD;
            float gcdPercent = gcdElapsed / gcdTotal;
            float castTotal = DataStore.Action->TotalCastTime;
            float castElapsed = DataStore.Action->ElapsedCastTime;
            float castPercent = castElapsed / castTotal;
            float slidecastStart = (castTotal - 0.5f) / castTotal;
            int triangleSize = (int)Math.Min(ui.w_size.X / 3, ui.w_size.Y / 3);
            int borderSize = triangleSize / 6;
            Vector4 red = new(1f, 0f, 0f, 1f);
            Vector4 green = new(0f, 1f, 0f, 1f);
            Vector4 bgCol = new(0f, 0f, 0f, .6f);

            // slidecast
            Vector2 slideTop = new(ui.w_cent.X, ui.w_cent.Y - triangleSize - borderSize);
            Vector2 slideLeft = slideTop + new Vector2(-triangleSize, triangleSize);
            Vector2 slideRight = slideTop + new Vector2(triangleSize, triangleSize);
            // queuelock
            Vector2 queueBot = new(slideTop.X,  ui.w_cent.Y + triangleSize + borderSize);
            Vector2 queueRight = queueBot - new Vector2(-triangleSize, triangleSize);
            Vector2 queueLeft = queueBot - new Vector2(triangleSize, triangleSize);

            Vector2 slideBGTop = slideTop - new Vector2(0f, borderSize);
            Vector2 slideBGLeft = slideLeft - new Vector2(1.75f * borderSize, - borderSize / 1.5f);
            Vector2 slideBGRight = slideRight + new Vector2(1.75f * borderSize, borderSize / 1.5f);

            // queuelock background
            Vector2 queueBGBot = queueBot + new Vector2(0f, borderSize);
            Vector2 queueBGRight = queueRight + new Vector2(1.75f * borderSize, - borderSize / 1.5f);
            Vector2 queueBGLeft = queueLeft - new Vector2(1.75f * borderSize, borderSize / 1.5f);

            bool cantSlide = castPercent != 0 && castPercent < slidecastStart;
            bool cantQueue = gcdPercent != 0 && gcdPercent < bar.QueueLock.LockPos;
            
            if (conf.SlidecastTriangleEnable && !(conf.OnlyGreenTriangles && cantSlide)) {
                ui.DrawAATriangle(slideBGTop, slideBGLeft, slideBGRight, bgCol);
                ui.DrawAATriangle(slideTop, slideLeft, slideRight, cantSlide ? red : green);
            }
            if (conf.QueuelockTriangleEnable && !(conf.OnlyGreenTriangles && cantQueue)) {
                ui.DrawAATriangle(queueBGBot, queueBGRight, queueBGLeft, bgCol);
                ui.DrawAATriangle(queueBot, queueRight, queueLeft, cantQueue ? red : green);
            }
        }
        
        
        public bool ShouldDraw(bool inCombat, bool noUI) {
            bool shouldShowTrianges = conf.FloatingTrianglesEnable && 
                                !noUI;
            bool showTriInCombat = conf.ShowOutOfCombat || inCombat;
            bool showTriWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                            (helper.idleTimerAccum < 
                                            helper.GCDTimeoutBuffer);

            return shouldShowTrianges && 
                (IsMoveable || 
                (showTriInCombat && 
                showTriWhenGCDNotRunning));
        }
        public string WindowName => "GCDTracker_SlideQueueIndicators";
        public bool IsMoveable => conf.WindowMoveableSQI;
    }
}