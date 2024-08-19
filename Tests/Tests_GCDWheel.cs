using GCDTracker;
using GCDTracker.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class Tests_GCDWheel
    {
        [TestMethod]
        public void Test_ogcd_queue() {
            //Weaponskill
            Assert.AreEqual((true,false,false), GetActionPressStatus(new MockAction(){
                TotalGCD = 2.312f
            }));
            //Queued skill
            Assert.AreEqual((false, true, false), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.301f,
                AnimationLock = 0.5178f,
                recast_group = 4
            }));
            //Executed Skill
            Assert.AreEqual((false, false, true), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.8190f,
                recast_group = 4
            }));
        }
        [TestMethod]
        public void Test_mudra_queue() {
            //Mudra
            Assert.AreEqual((true,false,false), GetActionPressStatus(new MockAction(){
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
                recast_group = 8,
                additional_recast = 57
            }));
            //Queued Ninjutsu
            Assert.AreEqual((true, true, false), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.2810f,
                TotalGCD = 0.5f,
                AnimationLock = 0.0689f
            }));
            //Executed Ninjutsu
            Assert.AreEqual((true, false, true), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                TotalGCD = 1.5f,
            }));
        }
        [TestMethod]
        public void Test_mudra_double_queue() {
            //Mudra 1
            Assert.AreEqual((true,false,false), GetActionPressStatus(new MockAction(){
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
                recast_group = 8,
                additional_recast = 57
            }));
            //Queued Mudra 2
            Assert.AreEqual((true, true, false), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.1818f,
                TotalGCD = 0.5f,
                AnimationLock = 0.168f,
                recast_group = 8,
                additional_recast = 57
            }));
            //Executed Mudra 2
            Assert.AreEqual((true, false, true), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                TotalGCD = 0.5f,
                AnimationLock = 0.35f,
            }));
            //Queued Ninjutsu
            Assert.AreEqual((true, true, false), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                ElapsedGCD = 0.007f,
                TotalGCD = 0.5f,
                AnimationLock = 0.342f,
            }));
            //Execute Ninjutsu
            Assert.AreEqual((true, false, true), GetActionPressStatus(new MockAction()
            {
                InQueue = true,
                TotalGCD = 1.5f
            }));
        }
        [TestMethod]
        public void Test_exact_slidegcd_end() {
            var abilityManager = AbilityManager.Instance;
            abilityManager.UpdateOGCDs(new Dictionary<float, AbilityManager.AbilityTiming>
            {
                { 0, new AbilityManager.AbilityTiming(0.35f, false) },
                { 0.5f, new AbilityManager.AbilityTiming(0.64f, false) }
            });
            var helper = new GCDHelper(null, null)
            {
                TotalGCD = 0.5f
            };
            helper.SlideGCDs(0.5f, true);

            Assert.AreEqual(1, abilityManager.ogcds.Count);
            Assert.AreEqual(0, abilityManager.ogcds.Keys.First());
            Assert.AreEqual(new AbilityManager.AbilityTiming(0.64f, false), abilityManager.ogcds.Values.First());
        }
        [TestMethod]
        public void Test_additional_weaponskills() {
            // Some skills have two recast groups, one for the skill itself and another with 57. Here is a list of some of these skills.
            // The test itself isn't that useful other than a list of skills that have this property.
            Assert.IsTrue(HelperMethods._isWeaponSkill(12, 57)); //Goring Blade
            Assert.IsTrue(HelperMethods._isWeaponSkill(6, 57)); //
            Assert.IsTrue(HelperMethods._isWeaponSkill(10, 57)); // Tsubame-gaeshi
            Assert.IsTrue(HelperMethods._isWeaponSkill(8, 57)); // Mudras
            Assert.IsTrue(HelperMethods._isWeaponSkill(6, 57)); // Drill
            Assert.IsTrue(HelperMethods._isWeaponSkill(7, 57)); // Hot shot
            Assert.IsTrue(HelperMethods._isWeaponSkill(8, 57)); // Air Anchor
            Assert.IsTrue(HelperMethods._isWeaponSkill(11, 57)); // Chainsaw
            Assert.IsTrue(HelperMethods._isWeaponSkill(12, 57)); // Flamethrower
            Assert.IsTrue(HelperMethods._isWeaponSkill(5, 57)); // Gnashing Fang
            Assert.IsTrue(HelperMethods._isWeaponSkill(8, 57)); // Standard Step
            Assert.IsTrue(HelperMethods._isWeaponSkill(19, 57)); // Technical Step
            Assert.IsTrue(HelperMethods._isWeaponSkill(4, 57));  // Soul Slice/Scythe
            Assert.IsTrue(HelperMethods._isWeaponSkill(18, 57));  // Phlegma
        }

        public static (bool, bool, bool) GetActionPressStatus(MockAction act)
        {
            var isWeaponSkill = HelperMethods._isWeaponSkill(act.recast_group, act.additional_recast ?? act.recast_group);
            var AddingToQueue = HelperMethods._isAddingToQueue(isWeaponSkill, act.InQueue, act.ElapsedGCD, act.TotalGCD, act.AnimationLock, act.ElapsedCastTime, act.TotalCastTime);
            var ExecutingQueued = act.InQueue && !AddingToQueue;
            return (isWeaponSkill, AddingToQueue, ExecutingQueued);
        }
    }
    public class MockAction
    {
        public bool InQueue = false;
        public float ElapsedGCD = 0;
        public float ElapsedCastTime = 0; //value?
        public float TotalCastTime = 0; //value?
        public float TotalGCD = 2.31f;
        public float AnimationLock = 0.5f;
        public int recast_group = 57;
        public int? additional_recast = null;
    }
}
