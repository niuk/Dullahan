namespace Dullahan {
    public class Maybe<T> {
        private Maybe() { }

        public class Nothing : Maybe<T> {
            public Nothing() {

            }
        }

        public class Just : Maybe<T> {
            public readonly T item;
            public Just(T item) {
                this.item = item;
            }
        }
    }
}