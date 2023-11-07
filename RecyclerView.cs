using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Pinch.RecyclerView
{
    /// <summary>
    /// Recycler view for Unity.
    /// List of elements, it use pooling to display items in the screen. So it's going to keep a small list instead of the full elements list.
    /// </summary>
    /// <typeparam name="T">T must be an extension of ViewHolder from RecyclerView.</typeparam>
    public abstract class RecyclerView<T> : MonoBehaviour, RecyclerView<T>.IAdapter
    {
        public float decelerationRate = 0.5f;
        public Vector2 spacing;
        public bool isReverse;
        public int poolSize = 3;
        public int cacheSize = 3;
        private Pool _pool;
        public readonly List<IViewHolderInfo> AttachedScrap = new();
        private readonly List<IViewHolderInfo> _cache = new();
        public abstract GameObject OnCreateViewHolder();
        public abstract void OnBindViewHolder(T holder, int i);
        public abstract int GetItemCount();
        protected LayoutManager<T> LayoutManager;


        private void Awake()
        {
            LayoutManager = new LayoutManager<T>(this);
            foreach (Transform child in transform) Destroy(child.gameObject);
            LayoutManager.Create();
            OnDataChange();
        }

        private void AddToAttachedScrap(IViewHolderInfo vh, bool up)
        {
            LayoutManager.AttachToGrid(vh, up);
            vh.ItemView.SetActive(true);
            AttachedScrap.Add(vh);
        }

        private IViewHolderInfo TryGetViewHolderForPosition(int position)
        {
            if (position < 0 || position >= GetItemCount()) return null;
            else
            {
                for (var i = 0; i < AttachedScrap.Count; i++)
                {
                    if (AttachedScrap[i].CurrentIndex != position) continue;
                    var v = AttachedScrap[i];
                    AttachedScrap.RemoveAt(i);
                    return v;
                }

                for (var i = 0; i < _cache.Count; i++)
                {
                    if (_cache[i].CurrentIndex != position) continue;
                    var v = _cache[i];
                    _cache.RemoveAt(i);
                    return v;
                }

                var vhRecycled = _pool.GetFromPool();
                if (vhRecycled != null)
                {
                    vhRecycled.Status = Status.Scrap;
                    vhRecycled.LastIndex = vhRecycled.CurrentIndex;
                    vhRecycled.CurrentIndex = position;
                    LayoutManager.AttachToGrid(vhRecycled, true);
                    OnBindViewHolder((T)Convert.ChangeType(vhRecycled, typeof(T)), vhRecycled.CurrentIndex);
                    return vhRecycled;
                }

                IViewHolderInfo vh =
                    (ViewHolder<T>)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder() });
                vh.CurrentIndex = position;
                vh.LastIndex = position;
                vh.Status = Status.Scrap;
                LayoutManager.AttachToGrid(vh, true);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), vh.CurrentIndex);
                return vh;
            }
        }

        private void ThrowAttachedScrapToCache()
        {
            foreach (var vh in AttachedScrap) ThrowToCache(vh);
        }

        public void UpdateScrap()
        {
            var firstPosition = LayoutManager.GetFirstPosition();
            var tmpScrap = new List<IViewHolderInfo>();

            for (var i = firstPosition - 1; i < firstPosition + LayoutManager.GetScreenListSize() + 1; i++)
            {
                var vh = TryGetViewHolderForPosition(i);
                if (vh != null) tmpScrap.Add(vh);
            }

            ThrowAttachedScrapToCache();
            AttachedScrap.Clear();
            AttachedScrap.AddRange(tmpScrap);
        }

        public override string ToString()
        {
            var str = "";
            str += "Attached: {";
            str = AttachedScrap.Aggregate(str, (current, vh) => current + (vh.CurrentIndex + ","));
            str += "} Cache: {";
            str = _cache.Aggregate(str, (current, vh) => current + (vh.CurrentIndex + ","));
            str += "} Pool: {";
            str = _pool.Scrap.Aggregate(str, (current, vh) => current + (vh.CurrentIndex + ","));
            str += "}";
            return str;
        }

        private void ThrowToPool(IViewHolderInfo vh)
        {
            vh.Status = Status.Recycled;
            vh.ItemView.SetActive(false);
            var recycled = _pool.Throw(vh);
            recycled?.Destroy();
        }

        private void ThrowToCache(IViewHolderInfo viewHolder)
        {
            viewHolder.Status = Status.Cache;
            _cache.Add(viewHolder);
            if (_cache.Count <= cacheSize) return;
            ThrowToPool(_cache[0]);
            _cache.RemoveAt(0);
        }

        private void Clear()
        {
            LayoutManager.Clear();
            AttachedScrap.Clear();
            _pool = null;
            _cache.Clear();
        }

        public void OnDataChange(int pos = 0)
        {
            LayoutManager.IsCreating = true;
            if (pos < 0 || pos > GetItemCount()) return;
            Clear();
            _pool = new Pool(poolSize, cacheSize);
            if (GetItemCount() > 0)
            {
                IViewHolderInfo vh = (ViewHolder<T>)Activator.CreateInstance(typeof(T), new object[] { OnCreateViewHolder() });
                vh.CurrentIndex = pos;
                vh.LastIndex = pos;
                vh.Status = Status.Scrap;
                AddToAttachedScrap(vh, true);
                LayoutManager.SetPositionViewHolder(vh);
                OnBindViewHolder((T)Convert.ChangeType(vh, typeof(T)), pos);
                LayoutManager.OnDataChange(vh.ItemView, pos);
                var attachedScrapSize = LayoutManager.GetScreenListSize() + 1;
                for (var i = pos + 1; i < attachedScrapSize + pos; i++)
                {
                    if (i >= GetItemCount()) continue;
                    var vh2 = (IViewHolderInfo)Activator.CreateInstance(typeof(T),
                        new object[] { OnCreateViewHolder() });
                    vh2.CurrentIndex = i;
                    vh2.LastIndex = i;
                    vh2.Status = Status.Scrap;
                    AddToAttachedScrap(vh2, true);
                    LayoutManager.SetPositionViewHolder(vh2);
                    OnBindViewHolder((T)Convert.ChangeType(vh2, typeof(T)), i);
                }

                LayoutManager.ClampList();
            }

            LayoutManager.IsCreating = false;
        }

        private class Pool
        {
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            private int _poolSize;
            [UsedImplicitly] private int _cacheSize;

            public Pool(int poolSize, int cacheSize)
            {
                _poolSize = poolSize;
                _cacheSize = cacheSize;
            }

            public readonly Queue<IViewHolderInfo> Scrap = new();

            [UsedImplicitly]
            public bool IsFull() => Scrap.Count >= _poolSize;

            public IViewHolderInfo GetFromPool() => Scrap.Count > 0 ? Scrap.Dequeue() : null;

            public IViewHolderInfo Throw(IViewHolderInfo vh)
            {
                if (Scrap.Count < _poolSize)
                {
                    vh.Status = Status.Recycled;
                    Scrap.Enqueue(vh);
                }
                else
                {
                    vh.Status = Status.Recycled;
                    var recycled = Scrap.Dequeue();
                    Scrap.Enqueue(vh);
                    return recycled;
                }

                return null;
            }
        }

        [UsedImplicitly]
        private interface IAdapter
        {
            [UsedImplicitly]
            GameObject OnCreateViewHolder();

            [UsedImplicitly]
            void OnBindViewHolder(T holder, int i);

            [UsedImplicitly]
            int GetItemCount();
        }
    }
}