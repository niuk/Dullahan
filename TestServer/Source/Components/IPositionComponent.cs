namespace TestGame {
    public interface IPositionComponent : Dullahan.ECS.IComponent {
        int x { get; set; }
        int y { get; set; }
    }
}
