namespace Dullahan {
    public sealed class Empty {
        private Empty() { }
    }

    public sealed class Unit {
        public static Unit unit = new Unit();
        private Unit() { }
    }

    public class Maybe<T> {
        private Maybe() { }

        public sealed class Nothing : Maybe<T> {
            public Nothing() { }
        }

        public sealed class Just : Maybe<T> {
            public readonly T item;

            public Just(T item) {
                this.item = item;
            }
        }
    }

    public class Either<L, R> {
        private Either() { }

        public sealed class Left : Either<L, R> {
            public readonly L item;

            public Left(L item) {
                this.item = item;
            }
        }

        public sealed class Right : Either<L, R> {
            public readonly R item;

            public Right(R item) {
                this.item = item;
            }
        }
    }
}