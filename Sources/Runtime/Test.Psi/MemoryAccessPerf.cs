// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Psi.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MemoryAccessPerf
    {
        private delegate T PtrCast<T>(IntPtr ptr);

        // [TestMethod, Timeout(60000)]
        public unsafe void BufferEx_Check_Read_Inlined()
        {
            // to see results, enable DumpStack in MemoryAccess.il
            var bytes = new byte[16];
            BufferEx.Read<int>(bytes, 0);

            var array = new UnmanagedArray<int>(10);
            array.GetRef(0);
            array.UncheckedGet(0);
            array.UncheckedSet(0, 0);
            var x = array[0];
            array[0] = x; // this does not get inlined
        }

        [TestMethod]
        [Timeout(60000)]
        public unsafe void BufferEx_Read_Managed_Perf()
        {
            var iterations = 100000;
            var count = 256;
            var bytes = new byte[count * 16];
            int value = 1;

            // warm-up
            BufferEx.SizeOf<int>();
            value = BufferEx.Read<int>(bytes, 0);

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    value = BufferEx.Read<int>(bytes, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read " + sw.ElapsedTicks);

            var items = new int[256];
            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    ReadBaseline(ref value, items, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read T from T[] baseline " + sw.ElapsedTicks);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    ReadBaseline(ref value, bytes, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read int baseline " + sw.ElapsedTicks);

            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(256 * 16);
            BufferEx.Read<int>(ptr);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    BufferEx.Read<int>(ptr + j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read* " + sw.ElapsedTicks);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    ReadBaseline(ref value, ptr, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read* int baseline " + sw.ElapsedTicks);

            var rgbValue = default(RGB);
            BufferEx.Read<RGB>(bytes, 0);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    rgbValue = BufferEx.Read<RGB>(bytes, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read rgb " + sw.ElapsedTicks);

            BufferEx.Read<RGB>(bytes, 0);
            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    BufferEx.Read<RGB>(bytes, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read ret rgb " + sw.ElapsedTicks);

            BufferEx.Read<RGB>(ptr);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    BufferEx.Read<RGB>(ptr + j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read* rgb" + sw.ElapsedTicks);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    ReadBaseline(ref rgbValue, bytes, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read rgb baseline " + sw.ElapsedTicks);

            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    ReadBaseline(ref rgbValue, ptr, j);
                }
            }

            sw.Stop();
            Console.WriteLine("Read* rgb baseline " + sw.ElapsedTicks);

            ReadBaseline(ref rgbValue, bytes, 0, p => *(RGB*)p);
            sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    ReadBaseline(ref rgbValue, bytes, j, p => *(RGB*)p);
                }
            }

            sw.Stop();
            Console.WriteLine("Read rgb del baseline " + sw.ElapsedTicks);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReadBaseline(ref RGB target, byte[] source, int index)
        {
            if (source.Length < index + sizeof(RGB))
            {
                throw new IndexOutOfRangeException();
            }

            fixed (byte* start = source)
            {
                target = *(RGB*)(start + index);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReadBaseline(ref int target, int[] source, int index)
        {
            target = source[index];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReadBaseline(ref int target, byte[] source, int index)
        {
            fixed (byte* src = source)
            {
                target = *(int*)(src + index);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReadBaseline(ref int target, IntPtr source, int index)
        {
            target = *(int*)(source + index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReadBaseline(ref RGB target, IntPtr source, int index)
        {
            target = *(RGB*)(source + index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe RGB RGBCast(IntPtr ptr)
        {
            return *(RGB*)ptr;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReadBaseline<T>(ref T target, byte[] source, int index, PtrCast<T> cast)
        {
            if (source.Length < index + sizeof(RGB))
            {
                throw new IndexOutOfRangeException();
            }

            fixed (byte* start = source)
            {
                target = cast((IntPtr)start + index);
            }
        }

#pragma warning disable 0169 // The field 'MemoryAccessPerf.RGB.*' is never used
        private struct RGB
        {
            private int r;
            private int g;
            private int b;
        }
#pragma warning restore 0169
    }
}