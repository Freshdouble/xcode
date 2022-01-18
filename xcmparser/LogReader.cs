using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace xcmparser
{
    class LogReader
    {
        private StreamReader reader;
        private ulong lastTimestamp = 0;
        private string lastLine = string.Empty;
        public LogReader(StreamReader reader)
        {
            this.reader = reader;
            lastTimestamp = PreloadLine();
        }

        private ulong PreloadLine()
        {
            string line = reader.ReadLine();
            var parts = line.Split(';');
            lastLine = parts[1];
            return ulong.Parse(parts[0]);
        }

        public bool IsEndOfStream { get; private set; } = false;

        public string ReadLine(out ulong WaitDuration)
        {
            string ret = lastLine;
            ulong? newTime = null;
            if (!reader.EndOfStream)
            {
                newTime = PreloadLine();
            }
            WaitDuration = newTime.GetValueOrDefault(lastTimestamp + 100) - lastTimestamp;
            IsEndOfStream = reader.EndOfStream;
            return ret;
        }

        public byte[] ReadData(out ulong WaitDuration)
        {
            string line = ReadLine(out WaitDuration);
            return Convert.FromBase64String(line);
        }

        private static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }
    }
}
