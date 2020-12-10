using Dullahan.ECS;

namespace TestGame {
    public interface IClientWorld : IWorld {
        VisualizationSystem visualizationSystem { get; }

        void Tick(int previousTick, int nextTick);
    }
}
