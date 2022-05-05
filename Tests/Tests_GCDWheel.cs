using GCDTracker.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public unsafe class Tests_GCDWheel
    {
        [TestMethod]
        public void test_mudra_queue()
        {
            const uint job = 30;

            var first_mudra = GetActionPressStatus(job,new MockAction(){
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
                recast_group = 8
            });
            Assert.AreEqual((true, false, false), first_mudra);
            var queued_ninjutsu = GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.2810f,
                TotalGCD = 0.5f,
                AnimationLock = 0.0689f,
                recast_group = 57
            });
            Assert.AreEqual((true, true, false), queued_ninjutsu);
            var executed_ninjutsu = GetActionPressStatus(job, new MockAction()
            {
                InQueue = true,
                TotalGCD = 1.5f,
                AnimationLock = 0.64f,
                recast_group = 57
            });
            Assert.AreEqual((true, false, true), executed_ninjutsu);
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
        public float TotalGCD = 0;
        public float AnimationLock = 0.6f;
        public int recast_group = 57;
    }
}
