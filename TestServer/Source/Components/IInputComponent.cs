namespace TestGame {
    public interface IInputComponent : Dullahan.ECS.IComponent {
        int deltaX { get; set;  }
        int deltaY { get; set; }
    }
}
