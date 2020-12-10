using Dullahan.ECS;

namespace TestGame {
    public interface IServerWorld : IWorld {
        MovementSystem movementSystem { get; }

        void Tick(int previousTick, int nextTick);
    }
}