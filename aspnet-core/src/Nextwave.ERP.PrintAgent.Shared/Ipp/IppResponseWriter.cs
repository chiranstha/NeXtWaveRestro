using System.Buffers.Binary;
using System.Text;

namespace Nextwave.ERP.PrintAgent.Ipp;

public static class IppResponseWriter
{
    public static byte[] Create(int requestId, Guid jobId, PrintJobState state)
    {
        using var stream = new MemoryStream();
        stream.Write([0x02, 0x00, 0x00, 0x00]);
        Span<byte> requestIdBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(requestIdBytes, requestId);
        stream.Write(requestIdBytes);
        stream.WriteByte(0x02);
        WriteAttribute(stream, 0x21, "job-id", Math.Abs(jobId.GetHashCode()).ToString());
        WriteAttribute(stream, 0x42, "job-uri", $"urn:uuid:{jobId}");
        WriteAttribute(stream, 0x44, "job-state-message", state.ToString());
        stream.WriteByte(0x03);
        return stream.ToArray();
    }

    private static void WriteAttribute(Stream stream, byte tag, string name, string value)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var valueBytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[2];
        stream.WriteByte(tag);
        BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)nameBytes.Length));
        stream.Write(length);
        stream.Write(nameBytes);
        BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)valueBytes.Length));
        stream.Write(length);
        stream.Write(valueBytes);
    }
}
