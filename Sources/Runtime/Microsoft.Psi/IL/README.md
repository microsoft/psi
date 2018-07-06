# High-performance Memory Access

The `MemoryAccess` class is defined directly in IL (`MemoryAccess.il`) and is assembled as a prebuild step in the `Microsoft.Psi` project. It implements extremely high-performance inlined memory access and copying. It is used by `BufferEx` and `UnmanagedArray` to pack/unpack buffers with primitive types (much more efficiently than `BinaryReader/Writer`) and to copy arrays.

Performance and inlining are verified in [`MemoryAccessPerf.cs`](../../Test.Psi/MemoryAccessPerf.cs). To verify inlining, uncomment calls to `DumpStack()` in `MemoryAccess.il` and enable the `BufferEx_Check_Read_Inlined()` test.