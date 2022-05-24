using GCDTracker;
using GCDTracker.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class Tests_GCDWheel
    {
        [TestMethod]
        public void test_ogcd_queue()
        {
            const uint job = 22;

            //Weaponskill
            Assert.AreEqual((true,false,false),GetActionPressStatus(job,new MockAction(){
                TotalGCD = 2.312f
            }));
            //Queued skill
            Assert.AreEqual((false, true, false),GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.301f,
                AnimationLock = 0.5178f,
                recast_group = 4
            }));
            //Executed Skill
            Assert.AreEqual((false, false, true), GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.8190f,
                recast_group = 4
            }));
        }
        [TestMethod]
        public void test_mudra_queue()
        {
            const uint job = 30;

            //Mudra
            Assert.AreEqual((true,false,false),GetActionPressStatus(job,new MockAction(){
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
                recast_group = 8
            }));
            //Queued Ninjutsu
            Assert.AreEqual((true, true, false),GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.2810f,
                TotalGCD = 0.5f,
                AnimationLock = 0.0689f
            }));
            //Executed Ninjutsu
            Assert.AreEqual((true, false, true), GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                TotalGCD = 1.5f,
            }));
        }
        [TestMethod]
        public void test_mudra_double_queue()
        {
            const uint job = 30;

            //Mudra 1
            Assert.AreEqual((true,false,false),GetActionPressStatus(job,new MockAction(){
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
                recast_group = 8
            }));
            //Queued Mudra 2
            Assert.AreEqual((true, true, false),GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.1818f,
                TotalGCD = 0.5f,
                AnimationLock = 0.168f,
                recast_group = 8
            }));
            //Executed Mudra 2
            Assert.AreEqual((true, false, true), GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
            }));
            //Queued Ninjutsu
            Assert.AreEqual((true, true, false),GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.007f,
                TotalGCD = 0.5f,
                AnimationLock = 0.342f,
            }));
            //Execute Ninjutsu
            Assert.AreEqual((true, false, true),GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                TotalGCD = 1.5f
            }));
        }
        [TestMethod]
        public void test_exact_slidegcd_end() {
            GCDWheel wheel = new()
            {
                ogcds = new()
                {
                    { 0, (0.35f, false) },
                    { 0.5f, (0.64f, false) }
                },
                TotalGCD = 0.5f
            };
            wheel.SlideGCDs(0.5f, true);

            Assert.AreEqual(1, wheel.ogcds.Count);
            Assert.AreEqual(0, wheel.ogcds.Keys.First());
            Assert.AreEqual((0.64f, false), wheel.ogcds.Values.First());
        }

        public (bool, bool, bool) GetActionPressStatus(uint job, MockAction act)
        {
            var isWeaponSkill = HelperMethods._isWeaponSkill(act.recast_group, job);
            var AddingToQueue = HelperMethods._isAddingToQueue(isWeaponSkill, act.InQueue, act.ElapsedGCD, act.TotalGCD, act.AnimationLock);
            var ExecutingQueued = act.InQueue && !AddingToQueue;
            return (isWeaponSkill, AddingToQueue, ExecutingQueued);
        }
    }
    public class MockAction
    {
        public bool InQueue = false;
        public float ElapsedGCD = 0;
        public float TotalGCD = 2.31f;
        public float AnimationLock = 0.64f;
        public int recast_group = 57;
    }
}
