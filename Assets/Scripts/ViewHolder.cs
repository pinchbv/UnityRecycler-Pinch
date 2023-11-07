using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Pinch.RecyclerView
{
    public abstract class ViewHolder<T> : IViewHolderInfo
    {
        private GameObject _itemView;
        private RectTransform _rectTransform;
        private int _currentIndex;
        private long _timeStamp;

        int IViewHolderInfo.LastIndex { get; set; }

        int IViewHolderInfo.CurrentIndex
        {
            get => _currentIndex;
            set => _currentIndex = value;
        }

        GameObject IViewHolderInfo.ItemView
        {
            get => _itemView;
            set => _itemView = value;
        }

        RectTransform IViewHolderInfo.RectTransform
        {
            get => _rectTransform;
            set => _rectTransform = value;
        }

        Status IViewHolderInfo.Status { get; set; }

        protected ViewHolder(GameObject itemView)
        {
            _itemView = itemView;
            _rectTransform = itemView.GetComponent<RectTransform>();
        }

        [UsedImplicitly]
        protected void Start()
        {
        }

        [UsedImplicitly]
        public int GetAdapterPosition()
        {
            return _currentIndex;
        }

        private void Destroy()
        {
            Object.Destroy(_itemView);
        }

        private bool IsHidden()
        {
            return !IsVisibleFrom(_itemView.GetComponent<RectTransform>());
        }

        private static bool IsVisibleFrom(RectTransform rectTransform)
        {
            return CountCornersVisibleFrom(rectTransform) > 0;
        }

        private static int CountCornersVisibleFrom(RectTransform rectTransform)
        {
            var screenBounds = new Rect(0f, 0f, Screen.width, Screen.height);
            var objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);
            return objectCorners.Count(t => screenBounds.Contains(t));
        }

        [UsedImplicitly]
        private int CompareTo(ViewHolder<T> vh) =>
            vh._currentIndex > _currentIndex ? -1 : vh._currentIndex > _currentIndex ? 1 : 0;

        void IViewHolderInfo.Destroy() => Destroy();

        bool IViewHolderInfo.IsHidden() => IsHidden();


    }
}