using System;
using System.IO;
using System.Text;

namespace Veles.Core
{
    public static class MinecraftServerFile
    {
        public static void Write(string path, string name, string address)
        {
            using (var stream = File.Create(path))
            {
                WriteByte(stream, 10); WriteString(stream, string.Empty);
                WriteByte(stream, 9); WriteString(stream, "servers"); WriteByte(stream, 10); WriteInt(stream, 1);
                WriteByte(stream, 8); WriteString(stream, "name"); WriteString(stream, name);
                WriteByte(stream, 8); WriteString(stream, "ip"); WriteString(stream, address);
                WriteByte(stream, 1); WriteString(stream, "acceptTextures"); WriteByte(stream, 0);
                WriteByte(stream, 0); WriteByte(stream, 0);
            }
        }

        private static void WriteByte(Stream stream, byte value) { stream.WriteByte(value); }
        private static void WriteInt(Stream stream, int value) { var bytes = BitConverter.GetBytes(value); if (BitConverter.IsLittleEndian) Array.Reverse(bytes); stream.Write(bytes, 0, bytes.Length); }
        private static void WriteString(Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty); var length = (ushort)bytes.Length;
            var header = BitConverter.GetBytes(length); if (BitConverter.IsLittleEndian) Array.Reverse(header);
            stream.Write(header, 0, header.Length); stream.Write(bytes, 0, bytes.Length);
        }
    }
}
