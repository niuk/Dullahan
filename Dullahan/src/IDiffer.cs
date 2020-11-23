namespace Dullahan {
    public interface IDiffer<TDiffable, TDiff> {
        bool Diff(TDiffable left, TDiffable right, out TDiff diff);
        void Patch(ref TDiffable diffable, TDiff diff);
    }
}