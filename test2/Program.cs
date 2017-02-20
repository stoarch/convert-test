using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
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
        const string START_SERVER = "-ss";
        const string SERVER_PORT = "-sp";

        private static void HandleArgs(string[] args)
        {
            for(int i = 0; i < args.Length; i++ )
            {
                var item = args[i];
                switch( item )
                {
                    case WRITE_SMALL_TEST:
                        {
                            GenerateTestFile( 10 );
                            break;
                        }

                    case WRITE_MEDIUM_TEST:
                        {
                            GenerateTestFile( 100 );
                            break;
                        }

                    case WRITE_LARGE_TEST:
                        {
                            GenerateTestFile( 1000 );
                            break;
                        }

                    case START_SERVER:
                        {
                            StartWebServer();
                            break;
                        }
                }
            }
        }

        private static void StartWebServer()
        {
            WebServer ws = new WebServer(SendResponse, "http://localhost:8080/");
            ws.Run();
            Console.WriteLine("Webserver running on localhost:8080. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            var query = request.QueryString;
            var queryString = "";
            foreach (String item in query.Keys)
            {
                queryString += String.Format("name: {0} value: {1}\n", item, query.Get(item));
            }

            Console.WriteLine(String.Format("Request {0} is received", request.RawUrl));
            Console.WriteLine(String.Format("Request params: {0}", queryString));
            Console.WriteLine(String.Format("Request path {0}", request.Url.AbsolutePath));

            var path = request.Url.AbsolutePath;

            if (path == "/convert/sqlite")
            {
                var file = query.Get("file");

                if (file != null)
                {
                    ConvertFileToSqlite(file);
                    return string.Format("<html><body>Conversion ready</body></html>", DateTime.Now);
                }
            }

            return "<html><body>Invalid command</body></html>";
        }

        private static void ConvertFileToSqlite(string file)
        {
            var bytes = File.ReadAllBytes(file);

            Header hdr;
            TradeRecord tr;

            var dbFile = file + ".db3";

            PrepareDB(dbFile);

            try
            {

                int hdrSize = Marshal.SizeOf(typeof(Header));
                int trSize = Marshal.SizeOf(typeof(TradeRecord));

                var hdrBytes = new byte[hdrSize];
                var trBytes = new byte[trSize];
                int i = 0;

                var hdrReader = new MemoryReader(hdrBytes);

                Array.Copy(bytes, hdrBytes, hdrSize);
                hdr = hdrReader.ReadStruct<Header>();

                Console.Write("Converting file " + file + " [H");

                WriteHeaderToDB(dbFile, hdr);

                i = hdrSize;

                while (i < bytes.Length)
                {
                    Array.Copy(bytes, i, trBytes, 0, trSize);

                    var trReader = new MemoryReader(trBytes);
                    tr = trReader.ReadStruct<TradeRecord>();

                    i += trSize;

                    Console.Write('T');


                    WriteTradeToDB(dbFile, tr);
                }

                Console.WriteLine("] done");
            }
            finally
            {
                CloseDB();
            }
        }

        private static void CloseDB()
        {
            connection.Close();
            connection = null;
        }

        private static void WriteTradeToDB(string dbFile, TradeRecord tr)
        {
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "INSERT INTO trade(id, account, comment, volume) "
                  + "VALUES (@id, @account, @comment, @volume);";
                cmd.Parameters.AddWithValue("@id", tr.id );
                cmd.Parameters.AddWithValue("@account", tr.account );
                cmd.Parameters.AddWithValue("@comment", tr.comment);
                cmd.Parameters.AddWithValue("@volume", tr.volume);

                cmd.ExecuteNonQuery();
            }
        }

        private static void WriteHeaderToDB(string dbFile, Header hdr)
        {
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "INSERT INTO header(type, version) "
                  + "VALUES (@type, @version);";
                cmd.Parameters.AddWithValue("@type", hdr.type );
                cmd.Parameters.AddWithValue("@version", hdr.version);
                cmd.ExecuteNonQuery();
            }
        }

        static SQLiteConnection connection;

        private static void PrepareDB(string dbFile)
        {
            SQLiteConnection.CreateFile(dbFile);

            SQLiteFactory factory = (SQLiteFactory)DbProviderFactories.GetFactory("System.Data.SQLite");
            connection = (SQLiteConnection)factory.CreateConnection();

            {
                connection.ConnectionString = "Data Source = " + dbFile;
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"CREATE TABLE [header] (
                    [id] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [type] char(16) NOT NULL,
                    [version] int NOT NULL
                    );";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"CREATE TABLE [trade] (
                    [iid] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                    [id] int NOT NULL,
                    [account] int NOT NULL,
                    [comment] char(64) NOT NULL,
                    [volume] real NOT NULL
                    );";
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
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
