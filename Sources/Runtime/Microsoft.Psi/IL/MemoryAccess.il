// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

.assembly extern netstandard as mscorlib // see: https://github.com/dotnet/coreclr/issues/10590
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )            
  .ver 2:0:0:0
}

.assembly Microsoft.Psi.IL {}
.module Microsoft.Psi.IL.dll

.class public abstract auto ansi sealed Microsoft.Psi.Common.MemoryAccess
       extends [mscorlib]System.Object
{
	.method public hidebysig static int32  SizeOf<valuetype .ctor ([mscorlib]System.ValueType) T>() cil managed aggressiveinlining
	{
	  // Code size       18 (0x12)
	  .maxstack  8
	  // call       void Microsoft.Psi.Common.MemoryAccess::DumpStack()
	  sizeof     !!T
	  ret
	}
	
	.method public hidebysig static !!T  ReadValue<valuetype .ctor ([mscorlib]System.ValueType) T>(native int source) cil managed aggressiveinlining
	{
	  // Code size       18 (0x12)
	  .maxstack  8
	  //call       void Microsoft.Psi.Common.MemoryAccess::DumpStack()
	  ldarg.0
	  call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
	  ldobj      !!T
	  ret
	} // end of method MemoryAccess`1::ReadValue

		
	// this flavor doesn't appear to get inlined (because of the return type?)
	.method public hidebysig static !!T&  ReadRef<valuetype .ctor ([mscorlib]System.ValueType) T>(native int source) cil managed aggressiveinlining
	{
	  // Code size       18 (0x12)
	  .maxstack  8
	  //call       void Microsoft.Psi.Common.MemoryAccess::DumpStack()
	  ldarg.0
	  call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
	  ret
	} // end of method MemoryAccess`1::ReadValue
		
	.method public hidebysig static void  ReadValue<valuetype .ctor ([mscorlib]System.ValueType) T>(!!T& target, native int source) cil managed aggressiveinlining
	{
	  // Code size       18 (0x12)
	  .maxstack  8
	  ldarg.0
	  ldarg.1
	  //call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
	  ldobj      !!T
	  stobj      !!T	
	  // call       void Microsoft.Psi.Common.MemoryAccess::DumpStack()
	  ret

	} // end of method MemoryAccess`1::ReadValue


	.method public hidebysig static void  WriteValue<valuetype .ctor ([mscorlib]System.ValueType) T>(!!T source,
													 native int target) cil managed aggressiveinlining
	{
		// Code size       18 (0x12)
	  .maxstack  8
	  IL_0000:  ldarg.1
	  IL_0001:  call       void* [mscorlib]System.IntPtr::op_Explicit(native int)
	  IL_0006:  ldarg.0
	  IL_000c:  stobj      !!T
	  //call       void Microsoft.Psi.Common.MemoryAccess::DumpStack()
	  IL_0011:  ret
	} // end of method MemoryAccess`1::WriteValue

	.method public hidebysig static void  CopyToArray<valuetype .ctor ([mscorlib]System.ValueType) T>(native int src,
													  !!T[] dest,
													  int32 targetIndex,
													  int32 elementCount) cil managed
	{
	  // Code size       69 (0x45)
	  .maxstack  5
	  .locals init (!!T& pinned V_0,
			   !!T[] V_1)
	  IL_0000:  ldarg.1
	  IL_0001:  dup
	  IL_0002:  stloc.1
	  IL_0003:  brfalse.s  IL_000a
	  IL_0005:  ldloc.1
	  IL_0006:  ldlen
	  IL_0007:  conv.i4
	  IL_0008:  brtrue.s   IL_000f
	  IL_000a:  ldc.i4.0
	  IL_000b:  conv.u
	  IL_000c:  stloc.0
	  IL_000d:  br.s       IL_0017
	  IL_000f:  ldloc.1
	  IL_0010:  ldc.i4.0
	  IL_0011:  ldelema    !!T
	  IL_0016:  stloc.0
	  IL_0017:  ldarga.s   src
	  IL_0019:  call       instance void* [mscorlib]System.IntPtr::ToPointer()
	  IL_001e:  ldloc.0
	  IL_001f:  conv.i
	  IL_0020:  ldarg.2
	  IL_0021:  conv.i
	  IL_0022:  sizeof     !!T
	  IL_0028:  mul
	  IL_0029:  add
	  IL_002a:  ldarg.1
	  IL_002b:  ldlen
	  IL_002c:  conv.i4
	  IL_002d:  sizeof     !!T
	  IL_0032:  mul
	  IL_0033:  conv.i8
	  IL_0034:  ldarg.3
	  IL_0035:  sizeof     !!T
	  IL_003a:  mul
	  IL_003b:  conv.i8
	  IL_003c:  call       void [mscorlib]System.Buffer::MemoryCopy(void*,
																	   void*,
																	   int64,
																	   int64)
	  IL_0041:  ldc.i4.0
	  IL_0042:  conv.u
	  IL_0043:  stloc.0
	  IL_0044:  ret

	} // end of method MemoryAccess`1::CopyToArray

	.method public hidebysig static void  CopyFromArray<valuetype .ctor ([mscorlib]System.ValueType) T>(!!T[] src,
														int32 srcIndex,
														native int dest,
														int32 destSizeInBytes,
														int32 elementCount) cil managed
	{
	  // Code size       62 (0x3e)
	  .maxstack  5
	  .locals init (!!T& pinned V_0,
			   !!T[] V_1)
	  IL_0000:  ldarg.0
	  IL_0001:  dup
	  IL_0002:  stloc.1
	  IL_0003:  brfalse.s  IL_000a
	  IL_0005:  ldloc.1
	  IL_0006:  ldlen
	  IL_0007:  conv.i4
	  IL_0008:  brtrue.s   IL_000f
	  IL_000a:  ldc.i4.0
	  IL_000b:  conv.u
	  IL_000c:  stloc.0
	  IL_000d:  br.s       IL_0017
	  IL_000f:  ldloc.1
	  IL_0010:  ldc.i4.0
	  IL_0011:  ldelema    !!T
	  IL_0016:  stloc.0
	  IL_0017:  ldloc.0
	  IL_0018:  conv.i
	  IL_0019:  ldarg.1
	  IL_001a:  conv.i
	  IL_001b:  sizeof     !!T
	  IL_0021:  mul
	  IL_0022:  add
	  IL_0023:  ldarga.s   dest
	  IL_0025:  call       instance void* [mscorlib]System.IntPtr::ToPointer()
	  IL_002a:  ldarg.3
	  IL_002b:  conv.i8
	  IL_002c:  ldarg.s    elementCount
	  IL_002e:  sizeof     !!T
	  IL_0033:  mul
	  IL_0034:  conv.i8
	  IL_0035:  call       void [mscorlib]System.Buffer::MemoryCopy(void*,
																	   void*,
																	   int64,
																	   int64)
	  IL_003a:  ldc.i4.0
	  IL_003b:  conv.u
	  IL_003c:  stloc.0
	  IL_003d:  ret

	} // end of method MemoryAccess`1::CopyFromArray

	.method assembly hidebysig static void  DumpStack() cil managed
	{
	  // Code size       11 (0xb)
	  .maxstack  8
	  IL_0000:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
	  IL_0005:  call       void [mscorlib]System.Console::WriteLine(object)
	  IL_000a:  ret
	} // end of method Generator::DumpStack

} // end of class Microsoft.Psi.Common.MemoryAccess`1
