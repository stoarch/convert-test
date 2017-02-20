using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace test2
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        public int version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string type;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TradeRecord
    {
        public int id;
        public int account;
        public double volume;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string comment;
    }

    class Program
    {
        static void Main(string[] args)
        {
            if( args.Length == 0)
            {
                ShowHelp();
                return;
            }

            HandleArgs(args);
        }

        const string WRITE_SMALL_TEST = "-wts";
        const string WRITE_MEDIUM_TEST = "-wtm";
        const string WRITE_LARGE_TEST = "-wtl";

        private static void HandleArgs(string[] args)
        {
            foreach (var item in args)
            {
                switch( item )
                {
                    case WRITE_SMALL_TEST:
                        {
                            GenerateTestFile( 10 );
                            return;
                        }

                    case WRITE_MEDIUM_TEST:
                        {
                            GenerateTestFile( 100 );
                            return;
                        }

                    case WRITE_LARGE_TEST:
                        {
                            GenerateTestFile( 1000 );
                            return;
                        }
                }
            }
        }

        private static void GenerateTestFile(int len)
        {
            Header hdr;
            TradeRecord tr;

            int hdrSize = Marshal.SizeOf(typeof(Header));
            int trSize = Marshal.SizeOf(typeof(TradeRecord));

            var hdrBytes = new byte[hdrSize];
            hdr.version = 1;
            hdr.type = "T1";

            Console.Write("Preparing file [");

            var hdrWriter = new MemoryWriter(hdrBytes);
            hdrWriter.WriteStruct(hdr);


            var trBytes = new byte[trSize];

            var fileBytes = new byte[hdrSize + trSize * len];
            hdrBytes.CopyTo(fileBytes, 0);

            Console.Write('H');

            var rnd = new Random();
            var pos = hdrSize;

            for (int i = 0; i < len; i++)
            {
                var trWriter = new MemoryWriter(trBytes);

                tr.account = rnd.Next(10000, 100000);
                tr.comment = "Comment" + i.ToString();
                tr.id = i;
                tr.volume = rnd.NextDouble()*rnd.Next(100);

                trWriter.WriteStruct(tr);

                trBytes.CopyTo(fileBytes, pos);
                pos += trSize;

                Console.Write('R');
            }

            File.WriteAllBytes(String.Format("test{0}.dat", rnd.Next(100000)), fileBytes);

            Console.WriteLine("] done");
        }

        public static void AppendAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Test server [v.0.1]");
            Console.WriteLine("Usage: test2 -wt[s|m|l]");
            Console.WriteLine("-wt - Write test file (s - small 10k, m - 100k, l - 1M)");
        }
    }
}
