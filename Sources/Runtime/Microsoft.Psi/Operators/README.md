# Operators

This directory contains the static methods that implement various \psi operators. The corresponding specialized component classes should be created in the Components directory instead.

## Namespaces, classes and files
The main static class for common operators is Microsoft.Psi.Operators. This is a partial class and spans several files, each file containing a certain subgroup of related methods.

For example, the `Joins.cs` file contains all `Join` overloads. Since most of the methods in the `Operators` class are extension methods, they become available and visible in Intellisense simply by using the `Microsoft.Psi` namespace. For this reason, operators that are less general should be implemented inside a different class and namespace, so that they don't pollute Intellisense when not needed.
