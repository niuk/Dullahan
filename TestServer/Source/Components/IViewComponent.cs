using Dullahan.ECS;

namespace TestGame {
    public interface IViewComponent : IComponent {
        char avatar { get; set; }
    }
}
