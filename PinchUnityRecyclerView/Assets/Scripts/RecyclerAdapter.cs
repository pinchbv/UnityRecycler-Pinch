using JetBrains.Annotations;
using UnityEngine;

namespace Pinch.RecyclerView
{
    public abstract class Adapter<T> : RecyclerView<T>, IDataObservable
    {
        public void NotifyDatasetChanged() => OnDataChange();

        [UsedImplicitly]
        public void ScrollBy(Vector2 pos) => LayoutManager.ScrollTo(pos);

        [UsedImplicitly]
        public void ScrollTo(int position) => LayoutManager.ScrollTo(position);

        [UsedImplicitly]
        public void SmoothScrollTo(int position) => LayoutManager.SmoothScrollTo(position);
    }
}