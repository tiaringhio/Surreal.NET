using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Surreal.Net;

/// <summary>
///     Encapsulates the logic of caching the last synchronously completed task of integer.
///     Used in classes like <see cref="MemoryStream" /> to reduce allocations.
/// </summary>
/// <remarks>
///     Source: https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/CachedCompletedInt32Task.cs
/// </remarks>
#if SURREAL_NET_INTERNAL
public
#else
internal
#endif
    struct CachedCompletedInt32Task {
    private Task<int>? _task;

    /// <summary> Gets a completed <see cref="Task{Int32}" /> whose result is <paramref name="result" />. </summary>
    /// <remarks> This method will try to return an already cached task if available. </remarks>
    /// <param name="result"> The result value for which a <see cref="Task{Int32}" /> is needed. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> GetTask(int result) {
        if (_task is { } task) {
            Debug.Assert(task.IsCompletedSuccessfully, "Expected that a stored last task completed successfully");
            if (task.Result == result) {
                return task;
            }
        }

        return _task = Task.FromResult(result);
    }
}

/// <summary>
/// </summary>
/// <remarks>
///     Based on: https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/IO/MemoryStream.cs
/// </remarks>
#if SURREAL_NET_INTERNAL
public
#else
internal
#endif
    sealed class PooledMemoryStream : Stream {
    private readonly MemoryPool<byte> _pool;
    private IMemoryOwner<byte> _buffer; // Either allocated internally or externally.
    private int _length; // Number of bytes within the memory stream

    private int _capacity; // length of usable portion of buffer for stream

    // Note that _capacity == _buffer.Length for non-user-provided byte[]'s
    private bool _expandable; // User-provided buffers aren't expandable.
    private bool _writable; // Can user write to this stream?
    private bool _isOpen; // Is this stream open or closed?

    private CachedCompletedInt32Task _lastReadTask; // The last successful task returned from ReadAsync

    private const int MemStreamMaxLength = int.MaxValue;

    public PooledMemoryStream() : this(null, 0) {
    }

    public PooledMemoryStream(int capacity) : this(null, capacity) {
    }

    public PooledMemoryStream(MemoryPool<byte> pool) : this(pool, 0) {
    }

    public PooledMemoryStream(
        MemoryPool<byte>? pool,
        int capacity) {
        _pool = pool ?? MemoryPool<byte>.Shared;
        _buffer = _pool.Rent(capacity);
        _capacity = capacity;
        _length = _capacity;
        _writable = true;
        _isOpen = true;
    }

    public override bool CanRead => _isOpen;

    public override bool CanSeek => _isOpen;

    public override bool CanWrite => _writable;


    private void EnsureNotClosed() {
        if (!_isOpen) {
            ThrowObjectDisposedException_StreamClosed(null);
        }
    }

    private static void ThrowObjectDisposedException_StreamClosed(string? objectName) {
        throw new ObjectDisposedException(objectName, "Cannot access a closed Stream.");
    }

    private void EnsureWriteable() {
        if (!CanWrite) {
            ThrowNotSupportedException_UnwritableStream();
        }
    }

    private static void ThrowNotSupportedException_UnwritableStream() {
        throw new NotSupportedException("Cannot write to this stream.");
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _buffer.Dispose();
            _isOpen = false;
            _writable = false;
            _expandable = false;
            // Don't set buffer to null - allow TryGetBuffer, GetBuffer & ToArray to work.
            _lastReadTask = default;
        }
    }

    // returns a bool saying whether we allocated a new array.
    private bool EnsureCapacity(int value) {
        // Check for overflow
        if (value < 0) {
            throw new IOException("Stream too long");
        }

        if (value > _capacity) {
            int newCapacity = Math.Max(value, 256);

            // We are ok with this overflowing since the next statement will deal
            // with the cases where _capacity*2 overflows.
            if (newCapacity < _capacity * 2) {
                newCapacity = _capacity * 2;
            }

            // We want to expand the array up to Array.MaxLength.
            // And we want to give the user the value that they asked for
            if ((uint)(_capacity * 2) > Array.MaxLength) {
                newCapacity = Math.Max(value, Array.MaxLength);
            }

            Capacity = newCapacity;
            return true;
        }

        return false;
    }

    public override void Flush() {
    }

    public override Task FlushAsync(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }

        try {
            Flush();
            return Task.CompletedTask;
        } catch (Exception ex) {
            return Task.FromException(ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> GetBuffer() {
        return _buffer.Memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetBufferSpan() {
        return _buffer.Memory.Span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetBufferSpan(int off) {
        return _buffer.Memory.Span.Slice(off);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetBufferSpan(
        int off,
        int len) {
        return _buffer.Memory.Span.Slice(off, len);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<byte> InternalReadSpan(int count) {
        EnsureNotClosed();

        int origPos = Pos;
        int newPos = origPos + count;

        if ((uint)newPos > (uint)_length) {
            Pos = _length;
            ThrowEndOfFileException();
        }

        Span<byte> span = GetBufferSpan(origPos, count);
        Pos = newPos;
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Memory<byte> InternalReadMemory(int count) {
        EnsureNotClosed();

        int origPos = Pos;
        int newPos = origPos + count;

        if ((uint)newPos > (uint)_length) {
            Pos = _length;
            ThrowEndOfFileException();
        }

        Memory<byte> span = GetBuffer().Slice(origPos, count);
        Pos = newPos;
        return span;
    }

    public ReadOnlyMemory<byte> ReadToEnd() {
        return InternalReadMemory(_length - Pos);
    }

    private static void ThrowEndOfFileException() {
        throw new EndOfStreamException("Cannot read beyond the end of the stream.");
    }

    // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
    internal int InternalEmulateRead(int count) {
        EnsureNotClosed();

        int n = _length - Pos;
        if (n > count) {
            n = count;
        }

        if (n < 0) {
            n = 0;
        }

        Debug.Assert(Pos + n >= 0, "_position + n >= 0"); // len is less than 2^31 -1.
        Pos += n;
        return n;
    }

    // Gets & sets the capacity (number of bytes allocated) for this stream.
    // The capacity cannot be set to a value less than the current length
    // of the stream.
    //
    public int Capacity {
        get {
            EnsureNotClosed();
            return _capacity;
        }
        set {
            // Only update the capacity if the MS is expandable and the value is different than the current capacity.
            // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
            if (value < Length) {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Capacity cannot be less than the length of the stream."
                );
            }

            EnsureNotClosed();

            if (!_expandable && value != Capacity) {
                throw new NotSupportedException("Cannot expand this stream.");
            }

            // MemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
            if (_expandable && value != _capacity) {
                if (value > 0) {
                    IMemoryOwner<byte> newBuffer = _pool.Rent(value);
                    if (_length > 0) {
                        GetBufferSpan(0, _length).CopyTo(newBuffer.Memory.Span);
                    }

                    _buffer.Dispose();
                    _buffer = newBuffer;
                } else {
                    _buffer.Dispose();
                }

                _capacity = value;
            }
        }
    }

    public override long Length {
        get {
            EnsureNotClosed();
            return _length;
        }
    }

    public override long Position {
        get {
            EnsureNotClosed();
            return Pos;
        }
        set {
            if (value < 0) {
                throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be negative.");
            }

            EnsureNotClosed();

            if (value > MemStreamMaxLength) {
                throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be greater than Int32.MaxValue.");
            }

            Pos = (int)value;
        }
    }

    public int Pos { get; private set; }

    public override int Read(
        byte[] buffer,
        int offset,
        int count) {
        ValidateBufferArguments(buffer, offset, count);
        EnsureNotClosed();

        int n = _length - Pos;
        if (n > count) {
            n = count;
        }

        if (n <= 0) {
            return 0;
        }

        Debug.Assert(Pos + n >= 0, "_position + n >= 0"); // len is less than 2^31 -1.

        Span<byte> currentBuffer = GetBufferSpan();

        if (n <= 8) {
            int byteCount = n;
            while (--byteCount >= 0) {
                buffer[offset + byteCount] = currentBuffer[Pos + byteCount];
            }
        } else {
            currentBuffer.Slice(Pos, n).CopyTo(buffer.AsSpan(offset, n));
        }

        Pos += n;

        return n;
    }

    public override int Read(Span<byte> buffer) {
        EnsureNotClosed();

        int n = Math.Min(_length - Pos, buffer.Length);
        if (n <= 0) {
            return 0;
        }

        GetBufferSpan(Pos, n).CopyTo(buffer);

        Pos += n;
        return n;
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) {
        ValidateBufferArguments(buffer, offset, count);

        // If cancellation was requested, bail early
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled<int>(cancellationToken);
        }

        try {
            int n = Read(buffer, offset, count);
            return _lastReadTask.GetTask(n);
        } catch (OperationCanceledException oce) {
            return Task.FromCanceled<int>(oce.CancellationToken);
        } catch (Exception exception) {
            return Task.FromException<int>(exception);
        }
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) {
        if (cancellationToken.IsCancellationRequested) {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        try {
            // ReadAsync(Memory<byte>,...) needs to delegate to an existing virtual to do the work, in case an existing derived type
            // has changed or augmented the logic associated with reads.  If the Memory wraps an array, we could delegate to
            // ReadAsync(byte[], ...), but that would defeat part of the purpose, as ReadAsync(byte[], ...) often needs to allocate
            // a Task<int> for the return value, so we want to delegate to one of the synchronous methods.  We could always
            // delegate to the Read(Span<byte>) method, and that's the most efficient solution when dealing with a concrete
            // MemoryStream, but if we're dealing with a type derived from MemoryStream, Read(Span<byte>) will end up delegating
            // to Read(byte[], ...), which requires it to get a byte[] from ArrayPool and copy the data.  So, we special-case the
            // very common case of the Memory<byte> wrapping an array: if it does, we delegate to Read(byte[], ...) with it,
            // as that will be efficient in both cases, and we fall back to Read(Span<byte>) if the Memory<byte> wrapped something
            // else; if this is a concrete MemoryStream, that'll be efficient, and only in the case where the Memory<byte> wrapped
            // something other than an array and this is a MemoryStream-derived type that doesn't override Read(Span<byte>) will
            // it then fall back to doing the ArrayPool/copy behavior.
            return new ValueTask<int>(
                MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> destinationArray)
                    ? Read(destinationArray.Array!, destinationArray.Offset, destinationArray.Count)
                    : Read(buffer.Span)
            );
        } catch (OperationCanceledException oce) {
            return new ValueTask<int>(Task.FromCanceled<int>(oce.CancellationToken));
        } catch (Exception exception) {
            return ValueTask.FromException<int>(exception);
        }
    }

    public override int ReadByte() {
        EnsureNotClosed();

        if (Pos >= _length) {
            return -1;
        }

        return GetBufferSpan()[Pos++];
    }

    public override void CopyTo(
        Stream destination,
        int bufferSize) {
        // Validate the arguments the same way Stream does for back-compat.
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        int originalPosition = Pos;

        // Seek to the end of the MemoryStream.
        int remaining = InternalEmulateRead(_length - originalPosition);

        // If we were already at or past the end, there's no copying to do so just quit.
        if (remaining > 0) {
            // Call Write() on the other Stream, using our internal buffer and avoiding any
            // intermediary allocations.
            destination.Write(GetBufferSpan().Slice(originalPosition, remaining));
        }
    }

    public override Task CopyToAsync(
        Stream destination,
        int bufferSize,
        CancellationToken cancellationToken) {
        // This implementation offers better performance compared to the base class version.

        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        // If canceled - return fast:
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }

        // Avoid copying data from this buffer into a temp buffer:
        // (require that InternalEmulateRead does not throw,
        // otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

        int pos = Pos;
        int n = InternalEmulateRead(_length - Pos);

        // If we were already at or past the end, there's no copying to do so just quit.
        if (n == 0) {
            return Task.CompletedTask;
        }

        // If destination is not a memory stream, write there asynchronously:
        if (!(destination is MemoryStream memStrDest)) {
            return destination.WriteAsync(GetBuffer().Slice(pos, n), cancellationToken).AsTask();
        }

        try {
            // If destination is a MemoryStream, CopyTo synchronously:
            memStrDest.Write(GetBufferSpan().Slice(pos, n));
            return Task.CompletedTask;
        } catch (Exception ex) {
            return Task.FromException(ex);
        }
    }


    public override long Seek(
        long offset,
        SeekOrigin loc) {
        EnsureNotClosed();

        if (offset > MemStreamMaxLength) {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                "offset is greater than the maximum length of a MemoryStream"
            );
        }

        switch (loc) {
        case SeekOrigin.Begin: {
                int tempPosition = unchecked((int)offset);
                if (offset < 0 || tempPosition < 0) {
                    throw new IOException("Attempted to seek before the beginning of the stream.");
                }

                Pos = tempPosition;
                break;
            }
        case SeekOrigin.Current: {
                int tempPosition = unchecked(Pos + (int)offset);
                if (unchecked(Pos + offset) < 0 || tempPosition < 0) {
                    throw new IOException("Attempted to seek before the beginning of the stream.");
                }

                Pos = tempPosition;
                break;
            }
        case SeekOrigin.End: {
                int tempPosition = unchecked(_length + (int)offset);
                if (unchecked(_length + offset) < 0 || tempPosition < 0) {
                    throw new IOException("Attempted to seek before the beginning of the stream.");
                }

                Pos = tempPosition;
                break;
            }
        default:
            throw new ArgumentException("Invalid SeekOrigin");
        }

        Debug.Assert(Pos >= 0, "_position >= 0");
        return Pos;
    }

    // Sets the length of the stream to a given value.  The new
    // value must be nonnegative and less than the space remaining in
    // the array, int.MaxValue - origin
    // Origin is 0 in all cases other than a MemoryStream created on
    // top of an existing array and a specific starting offset was passed
    // into the MemoryStream constructor.  The upper bounds prevents any
    // situations where a stream may be created on top of an array then
    // the stream is made longer than the maximum possible length of the
    // array (int.MaxValue).
    //
    public override void SetLength(long value) {
        if (value < 0 || value > int.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(value), "value is negative or greater than Int32.MaxValue");
        }

        EnsureWriteable();

        // Origin wasn't publicly exposed above.
        Debug.Assert(
            MemStreamMaxLength ==
            int.MaxValue
        ); // Check parameter validation logic in this method if this fails.

        if (value > int.MaxValue) {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "value is greater than the maximum length of a MemoryStream"
            );
        }

        int newLength = (int)value;
        bool allocatedNewArray = EnsureCapacity(newLength);
        if (!allocatedNewArray && newLength > _length) {
            GetBufferSpan(_length, newLength - _length).Clear();
        }

        _length = newLength;
        if (Pos > newLength) {
            Pos = newLength;
        }
    }

    public byte[] ToArray() {
        int count = _length;
        if (count == 0) {
            return Array.Empty<byte>();
        }

        byte[] copy = GC.AllocateUninitializedArray<byte>(count);
        GetBufferSpan(count).CopyTo(copy);
        return copy;
    }

    public override void Write(
        byte[] buffer,
        int offset,
        int count) {
        ValidateBufferArguments(buffer, offset, count);
        EnsureNotClosed();
        EnsureWriteable();

        int i = Pos + count;
        // Check for overflow
        if (i < 0) {
            throw new IOException("Stream too long");
        }

        if (i > _length) {
            bool mustZero = Pos > _length;
            if (i > _capacity) {
                bool allocatedNewArray = EnsureCapacity(i);
                if (allocatedNewArray) {
                    mustZero = false;
                }
            }

            if (mustZero) {
                GetBufferSpan(_length, i - _length).Clear();
            }

            _length = i;
        }

        Span<byte> currentBuffer = GetBufferSpan(Pos, count);
        if (count <= 8 && buffer.AsSpan().Overlaps(currentBuffer)) {
            int byteCount = count;
            while (--byteCount >= 0) {
                currentBuffer[byteCount] = buffer[offset + byteCount];
            }
        } else {
            buffer.AsSpan(offset, count).CopyTo(currentBuffer);
        }

        Pos = i;
    }


    public override void Write(ReadOnlySpan<byte> buffer) {
        EnsureNotClosed();
        EnsureWriteable();

        // Check for overflow
        int i = Pos + buffer.Length;
        if (i < 0) {
            throw new IOException("Stream too long");
        }

        if (i > _length) {
            bool mustZero = Pos > _length;
            if (i > _capacity) {
                bool allocatedNewArray = EnsureCapacity(i);
                if (allocatedNewArray) {
                    mustZero = false;
                }
            }

            if (mustZero) {
                GetBufferSpan(_length, i - _length).Clear();
            }

            _length = i;
        }

        buffer.CopyTo(GetBufferSpan().Slice(Pos, buffer.Length));
        Pos = i;
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) {
        ValidateBufferArguments(buffer, offset, count);

        // If cancellation is already requested, bail early
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }

        try {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        } catch (OperationCanceledException oce) {
            return Task.FromCanceled(oce.CancellationToken);
        } catch (Exception exception) {
            return Task.FromException(exception);
        }
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default) {
        if (cancellationToken.IsCancellationRequested) {
            return ValueTask.FromCanceled(cancellationToken);
        }

        try {
            // See corresponding comment in ReadAsync for why we don't just always use Write(ReadOnlySpan<byte>).
            // Unlike ReadAsync, we could delegate to WriteAsync(byte[], ...) here, but we don't for consistency.
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> sourceArray)) {
                Write(sourceArray.Array!, sourceArray.Offset, sourceArray.Count);
            } else {
                Write(buffer.Span);
            }

            return default;
        } catch (OperationCanceledException oce) {
            return new ValueTask(Task.FromCanceled(oce.CancellationToken));
        } catch (Exception exception) {
            return ValueTask.FromException(exception);
        }
    }

    public override void WriteByte(byte value) {
        EnsureNotClosed();
        EnsureWriteable();

        if (Pos >= _length) {
            int newLength = Pos + 1;
            bool mustZero = Pos > _length;
            if (newLength >= _capacity) {
                bool allocatedNewArray = EnsureCapacity(newLength);
                if (allocatedNewArray) {
                    mustZero = false;
                }
            }

            if (mustZero) {
                GetBufferSpan(_length, newLength - _length).Clear();
            }

            _length = newLength;
        }

        GetBufferSpan()[Pos++] = value;
    }

    // Writes this MemoryStream to another stream.
    public void WriteTo(Stream stream) {
        ArgumentNullException.ThrowIfNull(stream);

        EnsureNotClosed();

        stream.Write(GetBufferSpan().Slice(0, _length));
    }

    public ReadOnlyMemory<byte> GetConsumedBuffer() {
        return GetBuffer().Slice(0, Pos);
    }

    public string GetString(Encoding? encoding = null) {
        return (encoding ?? Encoding.Default).GetString(GetBufferSpan(0, Pos));
    }
}
