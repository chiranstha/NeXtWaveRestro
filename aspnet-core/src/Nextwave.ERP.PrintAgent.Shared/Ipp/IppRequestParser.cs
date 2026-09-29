using System.Buffers.Binary;
using System.Text;

namespace Nextwave.ERP.PrintAgent.Ipp;

public sealed class IppRequestParser
{
    public const int MaxDocumentBytes = 1024 * 1024;

    public IppPrintRequest Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 10 || payload[0] != 0x02 || payload[1] != 0x00)
        {
            throw new InvalidDataException("IPP 2.0 is required.");
        }

        if (BinaryPrimitives.ReadUInt16BigEndian(payload[2..4]) != 0x0002)
        {
            throw new InvalidDataException("Only the IPP Print-Job operation is supported.");
        }

        var requestId = BinaryPrimitives.ReadInt32BigEndian(payload[4..8]);
        var offset = 9;
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        while (offset < payload.Length && payload[offset] != 0x03)
        {
            var tag = payload[offset++];
            if (tag <= 0x0f)
            {
                continue;
            }

            EnsureRemaining(payload, offset, 2);
            var nameLength = BinaryPrimitives.ReadUInt16BigEndian(payload[offset..(offset + 2)]);
            offset += 2;
            EnsureRemaining(payload, offset, nameLength + 2);
            var name = Encoding.UTF8.GetString(payload.Slice(offset, nameLength));
            offset += nameLength;
            var valueLength = BinaryPrimitives.ReadUInt16BigEndian(payload[offset..(offset + 2)]);
            offset += 2;
            EnsureRemaining(payload, offset, valueLength);
            attributes[name] = Encoding.UTF8.GetString(payload.Slice(offset, valueLength));
            offset += valueLength;
        }

        if (offset >= payload.Length || payload[offset++] != 0x03)
        {
            throw new InvalidDataException("IPP end-of-attributes marker is missing.");
        }

        var document = payload[offset..].ToArray();
        if (document.Length == 0 || document.Length > MaxDocumentBytes)
        {
            throw new InvalidDataException("The ESC/POS document is empty or too large.");
        }

        attributes.TryGetValue("document-format", out var documentFormat);
        if (!string.Equals(documentFormat, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only ESC/POS application/octet-stream documents are supported.");
        }

        attributes.TryGetValue("job-name", out var jobName);
        if (string.IsNullOrWhiteSpace(jobName) || jobName.Length > 256)
        {
            throw new InvalidDataException("A valid job-name is required.");
        }

        return new IppPrintRequest(requestId, jobName, documentFormat!, document);
    }

    private static void EnsureRemaining(ReadOnlySpan<byte> payload, int offset, int length)
    {
        if (length < 0 || offset > payload.Length - length)
        {
            throw new InvalidDataException("The IPP request is truncated.");
        }
    }
}
