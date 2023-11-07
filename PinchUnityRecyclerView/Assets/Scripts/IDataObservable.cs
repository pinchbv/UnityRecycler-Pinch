using JetBrains.Annotations;

namespace Pinch.RecyclerView
{
    internal interface IDataObservable
    {
        [UsedImplicitly]
        void NotifyDatasetChanged();
    }
}