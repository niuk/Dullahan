namespace Dullahan {
    public class PrimitiveDiffer<T> : IDiffer<T, T> where T : struct {
        public Maybe<T> Diff(T left, T right) {
            if (Equals(left, right)) {
                return new Maybe<T>.Nothing();
            } else {
                return new Maybe<T>.Just(right);
            }
        }

        public T Patch(T diffable, T diff) {
            return diff;
        }
    }
}