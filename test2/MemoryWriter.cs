using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace test2
{
    class MemoryWriter : BinaryWriter
    {
        public MemoryWriter(byte[] buffer)
            : base(new MemoryStream(buffer))
        {
        }

        public void WriteStruct<T>(T t)
        {
            var sizeOfT = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(sizeOfT);
            Marshal.StructureToPtr(t, ptr, false);
            var bytes = new byte[sizeOfT];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);
            Marshal.FreeHGlobal(ptr);
            Write(bytes);
        }
    }
}
