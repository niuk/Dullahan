using Dullahan.ECS;

namespace TestGame {
    public interface ITimeComponent : IComponent {
        double deltaTime { get; set; }
    }
}
