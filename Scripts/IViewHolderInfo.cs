using UnityEngine;

namespace Pinch.RecyclerView
{
    public interface IViewHolderInfo
    {
        int LastIndex { get; set; }
        int CurrentIndex { get; set; }
        GameObject ItemView { get; set; }
        RectTransform RectTransform { get; set; }
        Status Status { get; set; }
        void Destroy();
        bool IsHidden();
    }
}