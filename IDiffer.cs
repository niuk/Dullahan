namespace Dullahan {
    public interface IDiffer<TDiffable, TDiff> {
        bool Diff(TDiffable left, TDiffable right, out TDiff diff);
        TDiffable Patch(TDiffable diffable, TDiff diff);
    }
}