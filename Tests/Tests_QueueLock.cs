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
            var conf = new Configuration();
            var go = new MockBarDecisionHelper(conf) {
                CurrentState = BarState.GCDOnly,
                CurrentPos = 0.5f
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0.8f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_Is_Pushed() {
            var conf = new Configuration();
            var go = new MockBarDecisionHelper(conf) { 
                CurrentState = BarState.ShortCast,
                CurrentPos = 0.9f
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0.9f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_LongCast() {
            var conf = new Configuration();
            var go = new MockBarDecisionHelper(conf) {
                CurrentState = BarState.LongCast,
                _GCDTotal = 2.5f,
                _CastTotal = 5.0f,
                CurrentPos = 0.3f 
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            float expectedLockPos = Math.Max(0.8f * (go.GCDTotal / go.CastTotal), go.CurrentPos);
            Assert.AreEqual(expectedLockPos, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_NonAbilityCast() {
            var conf = new Configuration();
            var go = new MockBarDecisionHelper(conf) {
                CurrentState = BarState.NonAbilityCast,
                CurrentPos = 0.5f
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_Idle_And_QueueLockWhenIdle_Is_True() {
            var conf = new Configuration() { BarQueueLockWhenIdle = true };
            var go = new MockBarDecisionHelper(conf) {
                CurrentState = BarState.Idle,
                CurrentPos = 0.5f 
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0.8f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_LockPos_When_State_Is_Idle_And_QueueLockWhenIdle_Is_False() {
            var conf = new Configuration() { BarQueueLockWhenIdle = false };
            var go = new MockBarDecisionHelper(conf) {
                CurrentState = BarState.Idle
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            queueLock.Update(bar_v);
            Assert.AreEqual(0f, queueLock.lockPos);
        }

        [TestMethod]
        public void Test_OnQueueLockReached_Alert() {
            var conf = new Configuration();
            var go = new MockBarDecisionHelper(conf) {
                CurrentState = BarState.GCDOnly,
                CurrentPos = 0.65f
            };
            var bar_v = new BarVertices(conf);
            var queueLock = new QueueLock(bar_v, go, conf);

            bool eventCalled = false;
            queueLock.OnQueueLockReached += () => eventCalled = true;

            queueLock.Update(bar_v);
            Assert.IsFalse(eventCalled);
            go.CurrentPos = 0.85f;
            queueLock.Update(bar_v);
            Assert.IsTrue(eventCalled);
        }
    }

    public class MockBarDecisionHelper(Configuration conf) : BarDecisionHelper(conf) {
        public float _GCDTotal = 2.5f;
        public float _CastTotal = 2.5f;
        public override float GCDTotal => _GCDTotal;
        public override float CastTotal => _CastTotal;
    }
}
