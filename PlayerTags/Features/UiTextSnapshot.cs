using System;

namespace PlayerTags.Features;

public sealed record UiTextSnapshot(byte[] Original, byte[] Written)
{
    public static ReadOnlySpan<byte> TextBytes(ReadOnlySpan<byte> bytes)
    {
        while (bytes.Length > 0 && bytes[^1] == 0) bytes = bytes[..^1];
        return bytes;
    }
    public bool StillOurs(ReadOnlySpan<byte> current) => current.SequenceEqual(Written);
    public byte[] SourceFor(ReadOnlySpan<byte> current) => StillOurs(current) ? Original : current.ToArray();
}
