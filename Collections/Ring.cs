using System;
using System.Collections;
using System.Collections.Generic;

namespace Dullahan {
    public class Ring<T> : IEnumerable<T> {
        private int bufferStart = 0;
        private T[] buffer = new T[1];

        public int Start { get; private set; } = 0;

        public int Count { get; private set; } = 0;

        public int End => Start + Count - 1;

        public T PeekStart() {
            if (Count > 0) {
                return buffer[bufferStart];
            } else {
                throw new InvalidOperationException();
            }
        }

        public T PeekEnd() {
            if (Count > 0) {
                return buffer[(bufferStart + Count - 1) % buffer.Length];
            } else {
                throw new InvalidOperationException();
            }
        }

        public T PopStart() {
            if (Count > 0) {
                T item = PeekStart();
                bufferStart = (bufferStart + 1) % buffer.Length;
                ++Start;
                --Count;
                return item;
            } else {
                throw new InvalidOperationException();
            }
        }

        public T PopEnd() {
            if (Count > 0) {
                T item = PeekEnd();
                --Count;
                return item;
            } else {
                throw new InvalidOperationException();
            }
        }

        public void PushStart(T item) {
            EnsureSize();
            bufferStart = bufferStart > 0 ? bufferStart - 1 : buffer.Length - 1;
            buffer[bufferStart] = item;
            --Start;
            ++Count;
        }

        public void PushEnd(T item) {
            EnsureSize();
            buffer[(bufferStart + Count) % buffer.Length] = item;
            ++Count;
        }

        private void EnsureSize() {
            if (Count == buffer.Length) {
                var newBuffer = new T[buffer.Length * 2];
                for (int i = 0; i < Count; ++i) {
                    newBuffer[i] = buffer[(bufferStart + i) % buffer.Length];
                }

                buffer = newBuffer;
                bufferStart = 0;
            }
        }

        public T this[int index] {
            get {
                if (Start <= index && index < Start + Count) {
                    return buffer[(bufferStart + (index - Start)) % buffer.Length];
                } else {
                    throw new IndexOutOfRangeException();
                }
            }

            set {
                if (Start <= index && index < Start + Count) {
                    buffer[(bufferStart + (index - Start)) % buffer.Length] = value;
                } else if (index == Start + Count) {
                    PushEnd(value);
                } else if (index == Start - 1) {
                    PushStart(value);
                } else {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<T> {
            private readonly Ring<T> ring;
            private int index;

            public Enumerator(Ring<T> ring) {
                this.ring = ring;
                index = -1;
            }

            public T Current => ring.buffer[(ring.bufferStart + index) % ring.buffer.Length];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext() {
                ++index;
                return index < ring.Count;
            }

            public void Reset() {
                index = -1;
            }
        }
    }
}