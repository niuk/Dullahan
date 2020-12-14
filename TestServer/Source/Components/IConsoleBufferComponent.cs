using Dullahan.ECS;

namespace TestGame {
    public interface IConsoleBufferComponent : IComponent {
        byte[,] consoleBuffer { get; set; }
    }
}