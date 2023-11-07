using JetBrains.Annotations;
using Pinch.RecyclerView;
using UnityEngine;
using UnityEngine.UI;

namespace Sample
{
    /// <summary>
    /// ViewHolder that binds a SampleData to a GameObject that conforms to SampleVh
    /// </summary>
    [UsedImplicitly]
    public class SampleVh : ViewHolder<SampleVh>
    {
        private readonly Text _textField;

        public SampleVh(GameObject itemView) : base(itemView)
        {
            // Init the VH fields to be bound at creation
            _textField = itemView.transform.Find("Text").GetComponent<Text>();
        }

        public void Bind(SampleData data)
        {
            // Update the bound VH fields when data is bound
            _textField.text = (
                $"<color=#FF0000>{data.Name}</color>" +
                $"<color=#00FF00>{data.Description}</color>" +
                $"<color=#0000FF>{data.Code}</color>"
            );
        }
    }
}