namespace Dullahan {
    public class ReferenceDiffer<T> : IDiffer<T, T> where T : class {
        public Maybe<T> Diff(T left, T right) {
            if (ReferenceEquals(left, right)) {
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