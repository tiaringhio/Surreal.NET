using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SurrealDB.Models;

namespace SurrealDB.Driver.Rest;

/// <summary>
///     The response from a query to the Surreal database via REST.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct DriverResponse : IResponse {
    internal static DriverResponse EmptyOk = new();

    // arraysegment is faster then readonlymemory, tho we do need expicit write protection
    private readonly ArraySegment<RawResult> _raw;
    private readonly RawResult _single;

    /// <summary>
    /// Initializes a new instance of <see cref="DriverResponse"/> from existing results, by copying the data.
    /// </summary>
    /// <param name="results">The existing data</param>
    /// <param name="owned">Whether the data is already owned; does not copy if true AND (<paramref name="results"/> is a array, OR <see cref="ArraySegment{T}"/>)!</param>
    public DriverResponse(IEnumerable<RawResult> results, bool owned = false) {
        // do not copy owned memory!
        if (owned && results is RawResult[] arr) {
            _raw = arr;
            return;
        }

        if (owned && results is ArraySegment<RawResult> seg) {
            _raw = seg;
            return;
        }

        var raw = results.ToArray(); // by now ToArray is faster, then manually checking the CopyTo method, by using the IListProvider interface
        // unbox single element array, or an empty array
        var rawS = raw.AsSpan(); // never use arrays directly!
        if (rawS.Length == 0) {
            // ignore empty arrays
            return;
        }
        if (rawS.Length == 1) {
            _single = rawS[0];
        }
        // keep boxed array
        _raw = new(raw);
    }

    private DriverResponse(ArraySegment<RawResult> owned) {
        _raw = owned;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DriverResponse FromOwned(ArraySegment<RawResult> owned) => new(owned); // protect ctor

    /// <summary>
    /// Boxes the result.
    /// </summary>
    /// <param name="result"></param>
    public DriverResponse(RawResult result) {
        _single = result;
    }

    /// <summary>
    /// Returns the memory occupied by the result data, returns an empty span if empty
    /// </summary>
    public ReadOnlySpan<RawResult> Raw => _raw.Array is null && !_single.IsDefault ? MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _single), 1) : _raw.AsSpan();

    private OkIterator Oks => new(GetEnumerator());
    IEnumerable<OkResult> IResponse.Oks => Oks;

    private ErrorIterator Errors => new(GetEnumerator());
    IEnumerable<ErrorResult> IResponse.Errors => Errors;
    public bool HasErrors => Errors.Any();
    public bool IsEmpty => _raw.Array is null && _single.IsDefault;

    public struct Enumerator : IEnumerator<IResult> {
        private readonly ref DriverResponse _rsp;
        private int _pos;

        internal Enumerator(in DriverResponse rsp) {
            // need to own, because we cant ensure the memory scope
            //_rsp = ref Unsafe.AsRef(in rsp);
            _rsp = rsp;
        }
        /// <summary>
        /// Exposed the reference to the current element.
        /// </summary>
        public ref readonly RawResult RawCurrent;

        public IResult Current => RawCurrent.ToResult();

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            int pos = _pos;
            if (pos < 0) {
                return false; // object disposed
            }
            if (pos < _rsp.Raw.Length) {
                RawCurrent = ref _rsp.Raw[pos];
                _pos = pos + 1;
            }
            return false;
        }

        public void Reset() {
            _pos = 0;
        }

        public void Dispose() {
            _pos = -1;
        }
    }

    public struct ErrorIterator : IEnumerator<ErrorResult>, IEnumerable<ErrorResult> {
        private Enumerator _en;
        private ErrorResult _cur;

        internal ErrorIterator(Enumerator en) {
            _en = en;
        }

        public bool MoveNext() {

            while (_en.MoveNext()) {
                if (!_en.RawCurrent.TryGetError(out ErrorResult err)) {
                    continue;
                }

                _cur = err;
                return true;
            }

            _cur = default;
            return false;
        }

        public void Reset() {
            _en.Reset();
        }

        public readonly ErrorResult Current => _cur;

        object IEnumerator.Current => _en.Current;

        public void Dispose() {
            _en.Dispose();
        }

        public ErrorIterator GetEnumerator() {
            return this; // copy
        }

        IEnumerator<ErrorResult> IEnumerable<ErrorResult>.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public struct OkIterator : IEnumerator<OkResult>, IEnumerable<OkResult> {
        private Enumerator _en;
        private OkResult _cur;

        internal OkIterator(Enumerator en) {
            _en = en;
        }

        public bool MoveNext() {
            while (_en.MoveNext()) {
                if (!_en.RawCurrent.TryGetValue(out OkResult ok)) {
                    continue;
                }

                _cur = ok;
                return true;
            }

            _cur = default;
            return false;
        }

        public void Reset() {
            _en.Reset();
        }

        public OkResult Current => _cur;

        object IEnumerator.Current => _en.Current;

        public void Dispose() {
            _en.Dispose();
        }

        public OkIterator GetEnumerator() {
            return this; // copy
        }

        IEnumerator<OkResult> IEnumerable<OkResult>.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<IResult> IEnumerable<IResult>.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
