// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.PsiStudio
{
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Visualization tests.
    /// </summary>
    [TestClass]
    public class VisualizationTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void TypeNameSimplification()
        {
            Assert.AreEqual("int", TypeSpec.Simplify("System.Int32"));
            Assert.AreEqual("string[]", TypeSpec.Simplify("System.String[]"));
            Assert.AreEqual("List<float[]>", TypeSpec.Simplify("System.Collections.Generic.List`1[[System.Single[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"));
            Assert.AreEqual("(int, string)", TypeSpec.Simplify("System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"));
            Assert.AreEqual("(List<string>, Dictionary<char, (int[], double)>)", TypeSpec.Simplify("System.ValueTuple`2[[System.Collections.Generic.List`1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.ValueTuple`2[[System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Double, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"));
        }
    }
}
