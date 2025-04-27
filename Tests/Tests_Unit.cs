using GCDTracker.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Text;
using Tests.Mocks;

namespace Tests;

[TestClass]
public class Tests_Unit {
    [TestMethod]
    public unsafe void TestReadStringFromPointer_WithValidString_ReturnsCorrectString() {
        byte[] buffer = "Blizzard III"u8.ToArray();
        fixed (byte* ptr = buffer) {
            byte* ptr2 = ptr;
            string result = HelperMethods.ReadStringFromPointer(&ptr2);
            Assert.AreEqual("Blizzard III", result);
        }
    }

    [TestMethod]
    public unsafe void TestReadStringFromPointer_WithNullPointer_ReturnsEmptyString() {
        byte* ptr = null;
        string result = HelperMethods.ReadStringFromPointer(&ptr);
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public unsafe void TestReadStringFromPointer_WithJaggedString_ReturnsCorrectString() {
        byte[] buffer = "Blizzard III\0"u8.ToArray();
        fixed (byte* ptr = buffer) {
            byte* ptr2 = ptr;
            string result = HelperMethods.ReadStringFromPointer(&ptr2);
            Assert.AreEqual("Blizzard III", result);
        }
    }

    [TestMethod]
    public unsafe void TestReadStringFromPointer_WithJapaneseString_ReturnsCorrectString() {
        byte[] buffer = "ブリザガ"u8.ToArray();
        fixed (byte* ptr = buffer) {
            byte* ptr2 = ptr;
            string result = HelperMethods.ReadStringFromPointer(&ptr2);
            Assert.AreEqual("ブリザガ", result);
        }
    }

    [TestMethod]
    public unsafe void TestSEString()
    {
        byte[] buffer = Encoding.UTF8.GetBytes("\u0002H\u0004�\u0002%\u0003\u0002I\u0004�\u0002&\u0003Vesper Bay Aetheryte Ticket\u0002I\u0002\u0001\u0003\u0002H\u0002\u0001\u0003");
        fixed (byte* ptr = buffer)
        {
            byte* ptr2 = ptr;
            string result = HelperMethods.ReadStringFromPointer(&ptr2);
            Assert.AreEqual("%\u0003&\u0003Vesper Bay Aetheryte Ticket", result);
        }
    }

    [TestMethod]
    public void TestFFXIVPathConfigured() {
        var ffxivPath = LuminaWrapper.GetFFXIVPath();
        Assert.IsFalse(string.IsNullOrEmpty(ffxivPath), "FFXIVPath is not configured");
    }

    [TestMethod]
    public void TestGetAbilityName_WithInvalidRowId_ReturnsUnknownMessage() {
        DataStore.Lumina = new LuminaWrapper();
        const uint invalidActionId = uint.MaxValue;
            
        var results = new[]            {
            HelperMethods.GetAbilityName(invalidActionId, ActionType.Action),
            HelperMethods.GetAbilityName(invalidActionId, ActionType.Mount),
            HelperMethods.GetAbilityName(invalidActionId, ActionType.Companion),
            HelperMethods.GetAbilityName(invalidActionId, ActionType.Item)
        };

        Assert.AreEqual("Unknown Ability", results[0], "Action should return 'Unknown Ability'");
        Assert.AreEqual("Unknown Mount", results[1], "Mount should return 'Unknown Mount'");
        Assert.AreEqual("Unknown Companion", results[2], "Companion should return 'Unknown Companion'");
        Assert.AreEqual("Unknown Item", results[3], "Item should return 'Unknown Item'");
    }
}