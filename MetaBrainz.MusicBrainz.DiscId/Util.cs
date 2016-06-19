using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId {
  
  internal static class Util {

    /// <summary>Marshals a structure from a byte array.</summary>
    /// <typeparam name="T">The type of structure to marshal.</typeparam>
    /// <param name="bytes">The raw representation of the structure to marshal.</param>
    /// <returns>The marshaled structure.</returns>
    public static T MarshalBytesToStructure<T>(byte[] bytes) where T : struct {
      var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      try {
        return (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
      }
      finally {
        handle.Free();
      }
    }

  }

}
