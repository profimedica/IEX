using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;


namespace IEX
{
    public class IexLinks
    {
        public string Link { get; set; }
        public string Feed { get; set; }
        public string Size { get; set; }
        public string Date { get; set; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 56, Pack = 1)]
    public struct IexHeader
    {
        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        [FieldOffset(0)]
        // 1 (0x1) Version of Transport specification
        public byte Version;

        [MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        [FieldOffset(1)]
        // Reserved byte
        public byte Reserved;

        [MarshalAs(UnmanagedType.U2, SizeConst = 1)]
        [FieldOffset(2)]
        // Unique identifier of the higher-layer protocol
        public short MessageProtocolID;

        [MarshalAs(UnmanagedType.U4, SizeConst = 1)]
        [FieldOffset(4)]
        // Identifies the stream of bytes/sequenced messages
        public int ChannelID;

        [MarshalAs(UnmanagedType.U4, SizeConst = 1)]
        [FieldOffset(8)]
        // Identifies the session 
        public int SessionID;

        [MarshalAs(UnmanagedType.U2, SizeConst = 1)]
        [FieldOffset(12)]
        // Byte length of the payload
        public short PayloadLength;

        [MarshalAs(UnmanagedType.U2, SizeConst = 1)]
        [FieldOffset(14)]
        // Number of messages in the payload
        public short MessageCount;

        [MarshalAs(UnmanagedType.U8, SizeConst = 1)]
        [FieldOffset(16)]
        // Byte offset of the data stream
        public long StreamOffset;

        [MarshalAs(UnmanagedType.U8, SizeConst = 1)]
        [FieldOffset(24)]
        // Sequence of the first message in the segment
        public long FirstMessageSequenceNumber;

        [MarshalAs(UnmanagedType.U8, SizeConst = 1)]
        [FieldOffset(32)]
        // Send time of segment
        public long SendTime;
    }

    public class IexDownloader
    {
        public string UrlBase = "https://api.iextrading.com/1.0/hist?date=";
        public string DestinationFolder = "C:\\Archive\\Stock\\";
        public string SevenZipPath = "C:\\Program Files\\7-Zip\\7z.exe";
        public IexDownloader()
        {
            if (!Directory.Exists(DestinationFolder))
            {
                Directory.CreateDirectory(DestinationFolder);
            }
        }
        public async Task<string> GetAsync(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public async Task<string> IexDownloadeDate(DateTime date)
        {
            if(date == DateTime.MinValue)
            {
                date = DateTime.Today.AddDays(-1).Date; // Make sure will be a working day, otherwise no data will be available to download
            }
            string filePath = DestinationFolder + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".gzip";
            if (File.Exists(filePath))
            {
                var deepFile = DestinationFolder + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "_IEXTP1_DEEP1.0.pcap";

                var bytes = File.ReadAllBytes(filePath);
                try
                {
                    var d = ByteArrayToStructure<IexHeader>(bytes);
                }
                catch (Exception e)
                {

                }
                var fr = IexParseToFile(deepFile, true);

                /*
                // Exception for large files, MS won't fix it!
                byte[] file = File.ReadAllBytes(filePath);
                byte[] decompressed = Decompress(file);
                string text = System.Text.ASCIIEncoding.ASCII.GetString(decompressed);
                File.WriteAllText(filePath + ".data", text);
                */
                ExtractFile(filePath, DestinationFolder);
                string parsed = IexParseToFile(filePath, true);
                return filePath + ".data";
            }
            // Create a new WebClient instance.
            using (WebClient myWebClient = new WebClient())
            {
                string links = await GetAsync(UrlBase + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(links))
                {
                    List<IexLinks> urls = JsonConvert.DeserializeObject<List<IexLinks>>(links);
                    myWebClient.DownloadFile(urls[0].Link, filePath);
                }
            }
            var f = IexParseToFile(DestinationFolder + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "_IEXTP1_DEEP1.0.pcap", true);
            return filePath;
        }

        T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            T stuff;
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return stuff;
        }

        private string IexParseToFile(string filePath, bool isDeep)
        {
            var bytes = File.ReadAllBytes(filePath);
            var d  = ByteArrayToStructure<IexHeader>(bytes);
            return filePath;
            // tops_version: float = 1.6,
            /*

        self.channel_id = b"\x01\x00\x00\x00"
        self.session_id = self._get_session_id(file_path)
        self.tp_header = (
            self.version
            + self.reserved
            + self.protocol_id
            + self.channel_id
            + self.session_id
        )
        self.messages_left = 0
        self.bytes_read = 0

        self.messages_types: Dict[AllMessages, bytes] = {
            messages.ShortSalePriceSale: b"\x50",
            messages.TradeBreak: b"\x42",
            messages.AuctionInformation: b"\x41",
            messages.TradeReport: b"\x54",
            messages.OfficialPrice: b"\x58",
            messages.SystemEvent: b"\x53",
            messages.SecurityDirective: b"\x44",
            messages.TradingStatus: b"\x48",
            messages.OperationalHalt: b"\x4f",
            messages.QuoteUpdate: b"\x51",
        }

    self.decoder = messages.MessageDecoder(version=tops_version)

    def __repr__(self) -> str:
        return f'Parser("{self.file_path}", tops={self.tops}, deep={self.deep})'

    def __iter__(self) -> Iterator:
        return self

    def __next__(self) -> AllMessages:
        """
        Meant to allow the user to use the Parser object in a loop to read
        through all messages in a pcap file similar to reading all lines in a
        file.
        Usage:
        for message in Parser(file_path):
            do_something(message)
        """
        return self.get_next_message()

    def __exit__(self, * args) -> None:
        self.file.close()

    def _load(self, file_path: str) -> BinaryIO:
        """
        Function to load a TOPS File into the parser.Simply returns a file
        object which other methods will iterate over.
        """
        return open(file_path, "rb")

    def _get_session_id(self, file_path: str) -> bytes:
        """
        The session ID is unique every day.Simply denotes the day.We use this
       to build the IEX Transport Protocol header.
       inputs:

           file_path   : path to pcap file to be decoded
       Returns:

           session_id  : binary encoded session ID
        """
        try:
            return self.session_id
       except AttributeError:

           iex_header_start = (
               self.version + self.reserved + self.protocol_id + self.channel_id
           )

           with open(file_path, "rb") as market_file:
                found = False
                i = 0
                while not found:
                    cur_chunk = market_file.read(1)
                    if cur_chunk[0] == iex_header_start[i]:
                        i += 1
                        if i == len(iex_header_start) :
                            found = True
                    else:
                        i = 0

                if found:
                    return market_file.read(4)
        raise ProtocolException("Session ID could not be found in the supplied file")

    def read_next_line(self) -> bytes:
        """
        Reads one line of the open pcap file, captures the len of that line,
        and returns that line to the caller.Not very useful to be honest, this

       may be deprecated in future versions.

       Inputs:

           None
       Returns:

           line    : binary encoded line from the pcap file
        """

       line = self.file.readline()

       self.bytes_read += len(line)
        if line:
            return line
        else:

           raise StopIteration("Reached end of PCAP file")

    def read_chunk(self, chunk: int = 1024) -> bytes:
        """
        Reads a single chunk of arbitrary size from the open file object and
       returns that chunk to the caller.
        Inputs:
            chunk   : determines the chunk size to be read from file
       Returns:
            data    : binary encoded chunk from the pcap file
        """
        data = self.file.read(chunk)
        self.bytes_read += len(data)
        if data:
            return data
        else:
            raise StopIteration("Reached end of PCAP file")

    def _seek_header(self) -> None:
        """
        Scans through the open file until it finds a complete version of the
       Transport Protocol Header which means that there is at least one
       message to parse.
        """
        found = False
       target_i = len(self.tp_header)
        i = 0
        while not found:
            cur_chunk = self.read_chunk(1)
            if cur_chunk [0] == self.tp_header [i]:
                i += 1
                if i == target_i:
                    found = True
            else:
                i = 0
        header_fmt = "<hhqqq"
        remaining_header = struct.unpack(header_fmt, self.read_chunk(28))
        self.cur_msg_payload_len = remaining_header[0]
        self.messages_left = remaining_header[1]
        self.cur_stream_offset = remaining_header[2]
        self.first_sequence_number = remaining_header[3]
        self.cur_send_time = datetime.fromtimestamp(
            remaining_header[4] / 10 * *9, tz = timezone.utc
        )

    def get_next_message(
        self, allowed: Optional[Union[List[AllMessages], Tuple[AllMessages]]] = None
    ) -> AllMessages:
        """
        Returns the next message in the pcap file.The user may optionally
        provide an 'allowed' argument to specify which type of messages they
        would like to retrieve.Please note that limiting the returned messages
        probably does not improve performance by that much, in fact tests have
        shown reduced rate of messages returned when allowed messages are
        specified (note the rate of messages returned is lower, but not the
        rate of messages analyzed).
        Inputs:
            allowed : types of messages to be returned
        Returns:
            message : decoded message from IEX file
        """
        if not isinstance(allowed, (list, tuple)) and allowed is not None:
            raise ValueError("allowed must be either a list or tuple")
        if allowed:
            allowed_types = [self.messages_types[a] [0] for a in allowed]

        while not self.messages_left:
            self._seek_header()

        self._read_next_message()
        while allowed is not None and self.message_type not in allowed_types:
            while not self.messages_left:
                self._seek_header()

            self._read_next_message()

        self.message = self.decoder.decode_message(
            self.message_type, self.message_binary
        )
        return self.message

    def _read_next_message(self) -> None:
        """
        Read next message from file - no return value, works by side effect.
        Note: using seek() to move past messages that we dont want to read
        doesn't seem to help performance. My theory is that using mmap should
        not help much either given that were typically reading the files from
        beginning to end sequentially.
        """
        message_len = struct.unpack("<h", self.read_chunk(2))[0]
    self.messages_left -= 1
        self.message_type = self.read_chunk(1)[0]
    self.message_binary = self.read_chunk(message_len - 1)
    */
        }

        public void ExtractFile(string sourceArchive, string destination)
        {
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                // pro.WindowStyle = ProcessWindowStyle.Hidden;
                pro.FileName = SevenZipPath;
                pro.Arguments = string.Format("x \"{0}\" -y -o\"{1}\"", sourceArchive, destination);
                Process x = Process.Start(pro);
                x.WaitForExit();
            }
            catch (System.Exception Ex)
            {
                //handle error
            }
        }

        public void DecompressLarge(string inputFilePath, string outputFilePath)
        {
            // Decompressing Large Gzip File not working using System.IO.Compression
            // https://stackoverflow.com/questions/37727233/decompressing-large-gzip-file-not-working-using-system-io-compression

            try
            {
                using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(inputFilePath))
                {
                    // Loop through the archive's files.
                    foreach (ZipEntry zip_entry in zip)
                    {
                        zip_entry.Extract(outputFilePath);
                    }
                    Console.WriteLine("Extraction completed for: " + outputFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error extracting archive: " + inputFilePath + ".\n" + ex.Message);
            }
        }

        static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
