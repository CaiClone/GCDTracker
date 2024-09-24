using GCDTracker;
using GCDTracker.UI;
using GCDTracker.UI.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests
{
    [TestClass]
    public class Tests_QueueLock
    {
        [TestMethod]
        public void Test_LockPos_When_State_Is_GCDOnly() {
            var go = new MockBarDecisionHelper() {
                CurrentState = BarState.GCDOnly,
                CurrentPos = 0.5f
            };
            var conf = new Configuration();
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0.8f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_Is_Pushed() {
            var go = new MockBarDecisionHelper() { 
                CurrentState = BarState.ShortCast,
                CurrentPos = 0.9f
            };
            var conf = new Configuration() { BarQueueLockSlide = true };
            var bar_v = new BarVertices();
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0.9f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_LongCast() {
            var info = new BarInfo() { CurrentPos = 0.3f };
            var conf = new Configuration();
            var go = new MockBarDecisionHelper() {
                CurrentState = BarState.LongCast,
                GCDTotal = 2.5f,
                CastTotal = 5.0f
            };
            var bar_v = new BarVertices();
            var queueLock = new QueueLock(info, bar_v, conf, go);

            queueLock.Update(bar_v);
            float expectedLockPos = Math.Max(0.8f * (go.GCDTotal / go.CastTotal), info.CurrentPos);
            Assert.AreEqual(expectedLockPos, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_NonAbilityCast() {
            var info = new BarInfo() { CurrentPos = 0.5f };
            var conf = new Configuration();
            var go = new BarDecisionHelper() { CurrentState = BarState.NonAbilityCast };
            var bar_v = new BarVertices();
            var queueLock = new QueueLock(info, bar_v, conf, go);

            queueLock.Update(bar_v);
            Assert.AreEqual(0f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_Idle_And_QueueLockWhenIdle_Is_True() {
            var info = new BarInfo() { CurrentPos = 0.5f };
            var conf = new Configuration() { BarQueueLockWhenIdle = true };
            var go = new MockBarDecisionHelper() { CurrentState = BarState.Idle };
            var bar_v = new BarVertices();
            var queueLock = new QueueLock(info, bar_v, conf, go);

            queueLock.Update(bar_v);
            Assert.AreEqual(0.8f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_Idle_And_QueueLockWhenIdle_Is_False() {
            var info = new BarInfo() { CurrentPos = 0.5f };
            var conf = new Configuration() { BarQueueLockWhenIdle = false };
            var go = new MockBarDecisionHelper() { CurrentState = BarState.Idle };
            var bar_v = new BarVertices();
            var queueLock = new QueueLock(info, bar_v, conf, go);

            queueLock.Update(bar_v);
            Assert.AreEqual(0f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_OnQueueLockReached_Alert() {
            var info = new BarInfo() { CurrentPos = 0.85f };
            var conf = new Configuration();
            var go = new MockBarDecisionHelper() { CurrentState = BarState.GCDOnly };
            var bar_v = new BarVertices();
            var queueLock = new QueueLock(info, bar_v, conf, go);

            bool eventCalled = false;
            queueLock.OnQueueLockReached += () => eventCalled = true;

            queueLock.Update(bar_v);
            Assert.IsTrue(eventCalled);
        }
    }

    public class MockBarDecisionHelper : BarDecisionHelper {
        public new BarState CurrentState { get; set; }
        public new float GCDTotal { get; set; } = 2.5f;
        public new float CastTotal { get; set; } = 2.5f;
    }
}
