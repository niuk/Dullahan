namespace TestGame {
    public interface IVelocityComponent : Dullahan.ECS.IComponent {
        float deltaX { get; set;  }
        float deltaY { get; set; }
        float speed { get; set; }
    }
}
