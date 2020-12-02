namespace Dullahan {
    public interface IDiffer<TDiffable, TDiff> {
        Maybe<TDiff> Diff(TDiffable left, TDiffable right);
        TDiffable Patch(TDiffable diffable, TDiff diff);
    }
}