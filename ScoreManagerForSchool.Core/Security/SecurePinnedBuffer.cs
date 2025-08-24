using System;
using System.Runtime.InteropServices;

namespace ScoreManagerForSchool.Core.Security
{
    // Small helper that allocates a managed byte[] and pins it.
    // On Dispose it clears the array and frees the GCHandle.
    public sealed class SecurePinnedBuffer : IDisposable
    {
        public byte[] Buffer { get; }
        private GCHandle _handle;
        private bool _disposed;

        public SecurePinnedBuffer(int size)
        {
            Buffer = new byte[size];
            _handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
        }

        public IntPtr Pointer => _handle.AddrOfPinnedObject();

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                if (Buffer != null) Array.Clear(Buffer, 0, Buffer.Length);
            }
            finally
            {
                if (_handle.IsAllocated) _handle.Free();
                _disposed = true;
            }
        }
    }
}
