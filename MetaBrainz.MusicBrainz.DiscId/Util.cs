using System;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class containing utility methods.</summary>
  internal static class Util {

    /// <summary>Marshals data from a byte array to a newly allocated managed object of the specified type.</summary>
    /// <typeparam name="T">The type of structure to marshal.</typeparam>
    /// <param name="bytes">The raw representation of the structure to marshal.</param>
    /// <returns>The marshaled structure.</returns>
    /// <exception cref="ArgumentException">When <typeparamref name="T"/> is a structure with automatic layout; it should use sequential or explicit layout.</exception>
    /// <exception cref="MissingMethodException">When <typeparamref name="T"/> does not have an accessible default constructor.</exception>
    public static T MarshalBytesToStructure<T>(byte[] bytes) where T : struct {
      var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      try {
        return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
      }
      finally {
        handle.Free();
      }
    }

  }

}
