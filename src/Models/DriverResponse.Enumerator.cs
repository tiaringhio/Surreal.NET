using System.Collections;

namespace SurrealDB.Models;

public readonly partial struct DriverResponse {
    public struct Enumerator : IEnumerator<RawResult> {
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

        public RawResult Current => RawCurrent;

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            int pos = _pos;
            if (pos < 0) {
                return false; // object disposed
            }

            if (pos == 0 && _rsp.IsSingle) {
                RawCurrent = ref _rsp._single;
                _pos = pos + 1;
                return true;
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
                if (!_en.RawCurrent.TryGetOk(out OkResult ok)) {
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

    IEnumerator<RawResult> IEnumerable<RawResult>.GetEnumerator() {
        return GetEnumerator();
    }
}
