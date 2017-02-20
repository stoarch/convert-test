using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace test2
{
    class MemoryReader : BinaryReader
    {
        public MemoryReader(byte[] buffer)
            : base(new MemoryStream(buffer))
        {
        }

        public T ReadStruct<T>()
        {
            var byteLength = Marshal.SizeOf(typeof(T));
            var bytes = ReadBytes(byteLength);
            var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var stt = (T)Marshal.PtrToStructure(
                pinned.AddrOfPinnedObject(),
                typeof(T));
            pinned.Free();
            return stt;
        }
    }
}
