namespace SKRIN_InsertToDBFromXML.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKRIN_InsertToDBFromXML.Console;

[TestClass]
public class UnitTests
{
    [TestMethod]
    public void IsCorrectDigits_Test()
    {
        for (int i = 1; i <= Program.MAX_ACTIONS; i++)
        {
            Assert.IsTrue(Program.IsCorrectDigit(i.ToString()));
        }
        for(int i = Program.MAX_ACTIONS + 1;  i < 10; i++)
        {
            Assert.IsFalse(Program.IsCorrectDigit(i.ToString()));
        }
    }

    [TestMethod]
    public void CorrectLengthOfRandomUsername_Test()
    {
        string username = Program.RandomUsername();
        Assert.AreEqual(Program.lengthOfRandomUsername, username.Length);
    }


}