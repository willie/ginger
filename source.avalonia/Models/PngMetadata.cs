using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Ginger.Models;

/// <summary>
/// Cross-platform PNG metadata reader for character card data.
/// Reads tEXt, zTXt chunks and EXIF data from PNG files.
/// </summary>
public static class PngMetadata
{
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// Result of extracting embedded data from a PNG file.
    /// Contains all supported format payloads.
    /// </summary>
    public struct EmbeddedData
    {
        public string? FaradayJson;    // From EXIF UserComment
        public string? TavernJsonV2;   // From 'chara' chunk
        public string? TavernJsonV3;   // From 'ccv3' chunk
        public string? GingerXml;      // From 'ginger' chunk
        public Dictionary<string, byte[]>? EmbeddedAssets; // From 'chara-ext-asset_' chunks

        public bool IsEmpty =>
            string.IsNullOrEmpty(FaradayJson) &&
            string.IsNullOrEmpty(TavernJsonV2) &&
            string.IsNullOrEmpty(TavernJsonV3) &&
            string.IsNullOrEmpty(GingerXml);
    }

    /// <summary>
    /// Extract all embedded character data from a PNG file.
    /// Reads EXIF (Faraday), tEXt/zTXt chunks (Tavern, Ginger), and embedded assets.
    /// </summary>
    public static EmbeddedData ExtractAllData(string filePath)
    {
        var result = new EmbeddedData();
        result.EmbeddedAssets = new Dictionary<string, byte[]>();

        // Read EXIF data (Faraday format)
        try
        {
            var exifData = new ExifData(filePath);
            if (exifData.GetTagValue(ExifTag.UserComment, out string? faradayJson, StrCoding.IdCode_UsAscii))
            {
                // Decode base64 if needed
                if (!string.IsNullOrEmpty(faradayJson) && !faradayJson.StartsWith('{'))
                {
                    if (faradayJson.Length > 0 && faradayJson.Length % 4 == 0 &&
                        Regex.IsMatch(faradayJson, @"^[a-zA-Z0-9\+/]*={0,2}$"))
                    {
                        byte[] byteArray = Convert.FromBase64String(faradayJson);
                        faradayJson = Encoding.UTF8.GetString(byteArray);
                    }
                }
                result.FaradayJson = faradayJson;
            }
        }
        catch
        {
            // EXIF reading failed, continue with other formats
        }

        // Read PNG chunks
        var metadata = ReadTextChunks(filePath);

        // Ginger XML
        if (metadata.TryGetValue("ginger", out string? gingerBase64))
        {
            try
            {
                byte[] byteArray = Convert.FromBase64String(gingerBase64);
                result.GingerXml = Encoding.UTF8.GetString(byteArray);
            }
            catch { }
        }

        // Tavern V2 (chara)
        if (metadata.TryGetValue("chara", out string? charaBase64))
        {
            try
            {
                byte[] byteArray = Convert.FromBase64String(charaBase64);
                result.TavernJsonV2 = Encoding.UTF8.GetString(byteArray);
            }
            catch { }
        }

        // Tavern V3 (ccv3)
        if (metadata.TryGetValue("ccv3", out string? ccv3Base64))
        {
            try
            {
                byte[] byteArray = Convert.FromBase64String(ccv3Base64);
                result.TavernJsonV3 = Encoding.UTF8.GetString(byteArray);
            }
            catch { }
        }

        // Embedded assets (chara-ext-asset_ prefix)
        const string assetPrefix = "chara-ext-asset_";
        foreach (var kvp in metadata)
        {
            if (kvp.Key.StartsWith(assetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string assetUri = kvp.Key.Substring(assetPrefix.Length);
                    // Handle RisuAI bug with extra colon
                    if (assetUri.StartsWith(":"))
                        assetUri = assetUri.Substring(1);

                    byte[] assetData = Convert.FromBase64String(kvp.Value);
                    result.EmbeddedAssets[assetUri] = assetData;
                }
                catch { }
            }
        }

        return result;
    }

    /// <summary>
    /// Extract all embedded character data from PNG bytes.
    /// </summary>
    public static EmbeddedData ExtractAllData(byte[] pngData)
    {
        var result = new EmbeddedData();
        result.EmbeddedAssets = new Dictionary<string, byte[]>();

        // Read EXIF data (Faraday format)
        try
        {
            using var stream = new MemoryStream(pngData);
            var exifData = new ExifData(stream);
            if (exifData.GetTagValue(ExifTag.UserComment, out string? faradayJson, StrCoding.IdCode_UsAscii))
            {
                // Decode base64 if needed
                if (!string.IsNullOrEmpty(faradayJson) && !faradayJson.StartsWith('{'))
                {
                    if (faradayJson.Length > 0 && faradayJson.Length % 4 == 0 &&
                        Regex.IsMatch(faradayJson, @"^[a-zA-Z0-9\+/]*={0,2}$"))
                    {
                        byte[] byteArray = Convert.FromBase64String(faradayJson);
                        faradayJson = Encoding.UTF8.GetString(byteArray);
                    }
                }
                result.FaradayJson = faradayJson;
            }
        }
        catch
        {
            // EXIF reading failed, continue with other formats
        }

        // Read PNG chunks
        var metadata = ReadTextChunks(pngData);

        // Ginger XML
        if (metadata.TryGetValue("ginger", out string? gingerBase64))
        {
            try
            {
                byte[] byteArray = Convert.FromBase64String(gingerBase64);
                result.GingerXml = Encoding.UTF8.GetString(byteArray);
            }
            catch { }
        }

        // Tavern V2 (chara)
        if (metadata.TryGetValue("chara", out string? charaBase64))
        {
            try
            {
                byte[] byteArray = Convert.FromBase64String(charaBase64);
                result.TavernJsonV2 = Encoding.UTF8.GetString(byteArray);
            }
            catch { }
        }

        // Tavern V3 (ccv3)
        if (metadata.TryGetValue("ccv3", out string? ccv3Base64))
        {
            try
            {
                byte[] byteArray = Convert.FromBase64String(ccv3Base64);
                result.TavernJsonV3 = Encoding.UTF8.GetString(byteArray);
            }
            catch { }
        }

        // Embedded assets (chara-ext-asset_ prefix)
        const string assetPrefix = "chara-ext-asset_";
        foreach (var kvp in metadata)
        {
            if (kvp.Key.StartsWith(assetPrefix, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string assetUri = kvp.Key.Substring(assetPrefix.Length);
                    if (assetUri.StartsWith(":"))
                        assetUri = assetUri.Substring(1);

                    byte[] assetData = Convert.FromBase64String(kvp.Value);
                    result.EmbeddedAssets[assetUri] = assetData;
                }
                catch { }
            }
        }

        return result;
    }

    /// <summary>
    /// Write EXIF UserComment to a PNG file (for Faraday format).
    /// </summary>
    public static bool WriteExifUserComment(string filePath, string payload)
    {
        try
        {
            var exifData = new ExifData(filePath);
            if (exifData.SetTagValue(ExifTag.UserComment, payload, StrCoding.IdCode_UsAscii))
            {
                exifData.Save(filePath);
                return true;
            }
        }
        catch
        {
        }
        return false;
    }

    public static Dictionary<string, string> ReadTextChunks(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return ReadTextChunks(stream);
    }

    public static Dictionary<string, string> ReadTextChunks(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return ReadTextChunks(stream);
    }

    public static Dictionary<string, string> ReadTextChunks(Stream stream)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Verify PNG signature
        var signature = new byte[8];
        if (stream.Read(signature, 0, 8) != 8)
            return result;

        for (int i = 0; i < 8; i++)
        {
            if (signature[i] != PngSignature[i])
                return result;
        }

        // Read chunks
        while (stream.Position < stream.Length)
        {
            // Read chunk length (big-endian)
            var lengthBytes = new byte[4];
            if (stream.Read(lengthBytes, 0, 4) != 4)
                break;

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);
            int length = BitConverter.ToInt32(lengthBytes, 0);

            // Read chunk type
            var typeBytes = new byte[4];
            if (stream.Read(typeBytes, 0, 4) != 4)
                break;
            string chunkType = Encoding.ASCII.GetString(typeBytes);

            // Read chunk data
            var chunkData = new byte[length];
            if (length > 0 && stream.Read(chunkData, 0, length) != length)
                break;

            // Skip CRC
            stream.Seek(4, SeekOrigin.Current);

            // Process text chunks
            if (chunkType == "tEXt")
            {
                var (keyword, text) = ParseTextChunk(chunkData);
                if (!string.IsNullOrEmpty(keyword))
                    result.TryAdd(keyword, text);
            }
            else if (chunkType == "zTXt")
            {
                var (keyword, text) = ParseCompressedTextChunk(chunkData);
                if (!string.IsNullOrEmpty(keyword))
                    result.TryAdd(keyword, text);
            }
            else if (chunkType == "IEND")
            {
                break;
            }
        }

        return result;
    }

    private static (string keyword, string text) ParseTextChunk(byte[] data)
    {
        // Find null separator
        int nullIndex = Array.IndexOf(data, (byte)0);
        if (nullIndex < 0)
            return ("", "");

        string keyword = Encoding.Latin1.GetString(data, 0, nullIndex);
        string text = Encoding.Latin1.GetString(data, nullIndex + 1, data.Length - nullIndex - 1);

        return (keyword, text);
    }

    private static (string keyword, string text) ParseCompressedTextChunk(byte[] data)
    {
        // Find null separator
        int nullIndex = Array.IndexOf(data, (byte)0);
        if (nullIndex < 0 || nullIndex + 2 > data.Length)
            return ("", "");

        string keyword = Encoding.Latin1.GetString(data, 0, nullIndex);

        // Skip compression method byte (should be 0 for zlib)
        int compressionMethod = data[nullIndex + 1];
        if (compressionMethod != 0)
            return (keyword, "");

        // Decompress the rest
        try
        {
            var compressedData = new byte[data.Length - nullIndex - 2];
            Array.Copy(data, nullIndex + 2, compressedData, 0, compressedData.Length);

            using var compressedStream = new MemoryStream(compressedData);
            // Skip zlib header (2 bytes)
            compressedStream.Seek(2, SeekOrigin.Begin);

            using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            deflateStream.CopyTo(resultStream);

            string text = Encoding.UTF8.GetString(resultStream.ToArray());
            return (keyword, text);
        }
        catch
        {
            return (keyword, "");
        }
    }

    public static bool WriteTextChunks(string inputPath, string outputPath, Dictionary<string, string> textChunks, bool compress = false)
    {
        try
        {
            byte[] originalData = File.ReadAllBytes(inputPath);
            byte[] modifiedData = WriteTextChunks(originalData, textChunks, compress);
            File.WriteAllBytes(outputPath, modifiedData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] WriteTextChunks(byte[] pngData, Dictionary<string, string> textChunks, bool compress = false)
    {
        using var inputStream = new MemoryStream(pngData);
        using var outputStream = new MemoryStream();

        // Copy PNG signature
        var signature = new byte[8];
        inputStream.Read(signature, 0, 8);
        outputStream.Write(signature, 0, 8);

        bool chunksInserted = false;

        while (inputStream.Position < inputStream.Length)
        {
            // Read chunk length
            var lengthBytes = new byte[4];
            if (inputStream.Read(lengthBytes, 0, 4) != 4)
                break;

            var lengthBytesCopy = (byte[])lengthBytes.Clone();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytesCopy);
            int length = BitConverter.ToInt32(lengthBytesCopy, 0);

            // Read chunk type
            var typeBytes = new byte[4];
            inputStream.Read(typeBytes, 0, 4);
            string chunkType = Encoding.ASCII.GetString(typeBytes);

            // Read chunk data and CRC
            var chunkData = new byte[length];
            if (length > 0)
                inputStream.Read(chunkData, 0, length);
            var crc = new byte[4];
            inputStream.Read(crc, 0, 4);

            // Insert our text chunks before IDAT
            if (!chunksInserted && chunkType == "IDAT")
            {
                foreach (var kvp in textChunks)
                {
                    WriteTextChunk(outputStream, kvp.Key, kvp.Value, compress);
                }
                chunksInserted = true;
            }

            // Skip existing tEXt/zTXt chunks with same keywords
            if ((chunkType == "tEXt" || chunkType == "zTXt") && length > 0)
            {
                int nullIndex = Array.IndexOf(chunkData, (byte)0);
                if (nullIndex > 0)
                {
                    string keyword = Encoding.Latin1.GetString(chunkData, 0, nullIndex);
                    if (textChunks.ContainsKey(keyword))
                        continue; // Skip this chunk
                }
            }

            // Write the chunk
            outputStream.Write(lengthBytes, 0, 4);
            outputStream.Write(typeBytes, 0, 4);
            if (length > 0)
                outputStream.Write(chunkData, 0, length);
            outputStream.Write(crc, 0, 4);
        }

        return outputStream.ToArray();
    }

    private static void WriteTextChunk(Stream stream, string keyword, string text, bool compress)
    {
        byte[] chunkData;
        string chunkType;

        if (compress)
        {
            chunkType = "zTXt";
            var keywordBytes = Encoding.Latin1.GetBytes(keyword);

            // Compress the text
            byte[] compressedText;
            using (var compressedStream = new MemoryStream())
            {
                // Write zlib header
                compressedStream.WriteByte(0x78);
                compressedStream.WriteByte(0x9C);

                using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, true))
                {
                    var textBytes = Encoding.UTF8.GetBytes(text);
                    deflateStream.Write(textBytes, 0, textBytes.Length);
                }

                compressedText = compressedStream.ToArray();
            }

            chunkData = new byte[keywordBytes.Length + 1 + 1 + compressedText.Length];
            Array.Copy(keywordBytes, 0, chunkData, 0, keywordBytes.Length);
            chunkData[keywordBytes.Length] = 0; // Null separator
            chunkData[keywordBytes.Length + 1] = 0; // Compression method
            Array.Copy(compressedText, 0, chunkData, keywordBytes.Length + 2, compressedText.Length);
        }
        else
        {
            chunkType = "tEXt";
            var keywordBytes = Encoding.Latin1.GetBytes(keyword);
            var textBytes = Encoding.Latin1.GetBytes(text);

            chunkData = new byte[keywordBytes.Length + 1 + textBytes.Length];
            Array.Copy(keywordBytes, 0, chunkData, 0, keywordBytes.Length);
            chunkData[keywordBytes.Length] = 0; // Null separator
            Array.Copy(textBytes, 0, chunkData, keywordBytes.Length + 1, textBytes.Length);
        }

        // Write length (big-endian)
        var lengthBytes = BitConverter.GetBytes(chunkData.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);
        stream.Write(lengthBytes, 0, 4);

        // Write type
        stream.Write(Encoding.ASCII.GetBytes(chunkType), 0, 4);

        // Write data
        stream.Write(chunkData, 0, chunkData.Length);

        // Calculate and write CRC
        var crcData = new byte[4 + chunkData.Length];
        Array.Copy(Encoding.ASCII.GetBytes(chunkType), 0, crcData, 0, 4);
        Array.Copy(chunkData, 0, crcData, 4, chunkData.Length);
        uint crc = CalculateCrc(crcData);
        var crcBytes = BitConverter.GetBytes(crc);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(crcBytes);
        stream.Write(crcBytes, 0, 4);
    }

    private static readonly uint[] CrcTable = GenerateCrcTable();

    private static uint[] GenerateCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                    c = 0xEDB88320 ^ (c >> 1);
                else
                    c >>= 1;
            }
            table[n] = c;
        }
        return table;
    }

    private static uint CalculateCrc(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFF;
    }
}
