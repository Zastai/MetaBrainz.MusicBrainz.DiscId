using System;
using System.Runtime.InteropServices;

namespace MetaBrainz.MusicBrainz.DiscId {

  /// <summary>Class containing utility methods.</summary>
  internal static class Util {

    /// <summary>Frees all substructures of a particular type, present in unmanaged memory.</summary>
    /// <typeparam name="T">The specific type of structure that <paramref name="pointer"/> points to.</typeparam>
    /// <param name="pointer">A pointer to the structure to destroy.</param>
    /// <exception cref="ArgumentException">When <typeparamref name="T"/> is a structure with automatic layout; it should use sequential or explicit layout.</exception>
    public static void DestroyStructure<T>(IntPtr pointer) where T : struct {
#if NETFX_TARGET && !NETFX_GE_4_5_1
      Marshal.DestroyStructure(pointer, typeof(T));
#else
      Marshal.DestroyStructure<T>(pointer);
#endif
    }

    /// <summary>Marshals data from a byte array to a newly allocated managed object of the specified type.</summary>
    /// <typeparam name="T">The type of structure to marshal.</typeparam>
    /// <param name="bytes">The raw representation of the structure to marshal.</param>
    /// <returns>The marshaled structure.</returns>
    /// <exception cref="ArgumentException">When <typeparamref name="T"/> is a structure with automatic layout; it should use sequential or explicit layout.</exception>
    /// <exception cref="MissingMethodException">When <typeparamref name="T"/> does not have an accessible default constructor.</exception>
    public static T MarshalBytesToStructure<T>(byte[] bytes) where T : struct {
      var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      try {
        return Util.MarshalPointerToStructure<T>(handle.AddrOfPinnedObject());
      }
      finally {
        handle.Free();
      }
    }

    /// <summary>Marshals data from an unmanaged block of memory to a newly allocated managed object of the specified type.</summary>
    /// <typeparam name="T">The type of structure to marshal.</typeparam>
    /// <param name="pointer">A pointer to the structure to marshal.</param>
    /// <returns>The marshaled structure.</returns>
    /// <exception cref="ArgumentException">When <typeparamref name="T"/> is a structure with automatic layout; it should use sequential or explicit layout.</exception>
    /// <exception cref="MissingMethodException">When <typeparamref name="T"/> does not have an accessible default constructor.</exception>
    public static T MarshalPointerToStructure<T>(IntPtr pointer) where T : struct {
#if NETFX_TARGET && !NETFX_GE_4_5_1
      return (T) Marshal.PtrToStructure(pointer, typeof(T));
#else
      return Marshal.PtrToStructure<T>(pointer);
#endif
    }

    /// <summary>Returns the (marshaled) size, in bytes, of a given structure.</summary>
    /// <typeparam name="T">The specific type of structure to determine the size of.</typeparam>
    /// <returns>The (marshaled) size of <typeparamref name="T"/>, in bytes.</returns>
    public static int SizeOfStructure<T>() where T : struct {
#if NETFX_TARGET && !NETFX_GE_4_5_1
      return Marshal.SizeOf(typeof(T));
#else
      return Marshal.SizeOf<T>();
#endif
    }

  }

}
