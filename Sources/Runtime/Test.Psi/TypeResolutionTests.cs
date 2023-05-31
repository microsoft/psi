// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TypeResolutionTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void TypeNameTest()
        {
            // primitive type
            string typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(int).AssemblyQualifiedName);
            Assert.AreEqual(typeof(int).FullName, typeName);
            Assert.AreEqual(typeof(int), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(int[]).AssemblyQualifiedName);
            Assert.AreEqual(typeof(int[]).FullName, typeName);
            Assert.AreEqual(typeof(int[]), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(int[,]).AssemblyQualifiedName);
            Assert.AreEqual(typeof(int[,]).FullName, typeName);
            Assert.AreEqual(typeof(int[,]), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(string).AssemblyQualifiedName);
            Assert.AreEqual(typeof(string).FullName, typeName);
            Assert.AreEqual(typeof(string), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(object).AssemblyQualifiedName);
            Assert.AreEqual(typeof(object).FullName, typeName);
            Assert.AreEqual(typeof(object), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(byte*).AssemblyQualifiedName);
            Assert.AreEqual(typeof(byte*).FullName, typeName);
            Assert.AreEqual(typeof(byte*), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(List<>).AssemblyQualifiedName);
            Assert.AreEqual(typeof(List<>).FullName, typeName);
            Assert.AreEqual(typeof(List<>), Type.GetType(typeName));

            // Note - Type.FullName does not remove the AQN from the inner type parameters of generic
            // types, so we won't test the result for equality with typeof(List<int>).FullName
            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(List<int>).AssemblyQualifiedName);
            Assert.AreEqual(typeof(List<int>), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(IEnumerable<int>).AssemblyQualifiedName);
            Assert.AreEqual(typeof(IEnumerable<int>), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(IDictionary<int, List<int>>).AssemblyQualifiedName);
            Assert.AreEqual(typeof(IDictionary<int, List<int>>), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(Func<int, double>).AssemblyQualifiedName);
            Assert.AreEqual(typeof(Func<int, double>), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName(typeof(NestedClass<List<int>>).AssemblyQualifiedName);
            Assert.AreEqual(typeof(NestedClass<List<int>>), Type.GetType(typeName));

            typeName = TypeResolutionHelper.RemoveAssemblyName("Namespace.TypeName, AssemblyName WithSpaces-v1.0.0.0, Version=1.0.0.0");
            Assert.AreEqual("Namespace.TypeName", typeName);

            typeName = TypeResolutionHelper.RemoveAssemblyName("Namespace.TypeName`2[[Nested.TypeName1, AssemblyName WithSpaces-v1.0.0.0, Version=1.0.0.0], [Nested.TypeName2[], AssemblyName, Culture=neutral]], AssemblyName, PublicKeyToken=null");
            Assert.AreEqual("Namespace.TypeName`2[[Nested.TypeName1], [Nested.TypeName2[]]]", typeName);
        }

        // empty class for type name testing
        private class NestedClass<T>
        {
        }
    }
}
