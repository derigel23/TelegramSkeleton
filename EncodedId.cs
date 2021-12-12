using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SimpleBase;

namespace Team23.TelegramSkeleton;

public readonly record struct EncodedId<T1, T2>(T1 Id, T2 SubId)
  where T1 : struct
  where T2 : struct
{
  public static readonly EncodedId<T1, T2> Empty = new();

  public override string ToString()
  {
    return this;
  }

  public static implicit operator string(EncodedId<T1, T2> id)
  {
    var tSize = Marshal.SizeOf<T1>();
    var idBytes = new byte[tSize + Marshal.SizeOf<T2>()];
    Unsafe.WriteUnaligned(ref idBytes[0], id.Id);
    Unsafe.WriteUnaligned(ref idBytes[tSize], id.SubId);
    return Base58.Flickr.Encode(idBytes);
  }

  public static implicit operator EncodedId<T1, T2>(string? encodedId)
  {
    var id = Empty;

    if (string.IsNullOrEmpty(encodedId))
      return id;

    var tSize = Marshal.SizeOf<T1>();
    var idBytes = new byte[tSize + Marshal.SizeOf<T2>()];

    if (Base58.Flickr.TryDecode(encodedId, idBytes, out var size) && size == idBytes.Length)
    {
      id = new EncodedId<T1, T2>(
        Unsafe.ReadUnaligned<T1>(ref idBytes[0]),
        Unsafe.ReadUnaligned<T2>(ref idBytes[tSize])
      );
    }

    return id;
  }
}