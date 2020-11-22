namespace Dullahan {
    public interface IWorld {
        int tick { get; }

        void Tick();
    }
}