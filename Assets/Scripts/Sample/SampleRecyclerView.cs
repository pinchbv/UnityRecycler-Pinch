using System.Collections.Generic;
using Pinch.RecyclerView;
using UnityEngine;

namespace Sample
{
    public class SampleRecyclerView : Adapter<SampleVh>
    {
        private readonly List<SampleData> _list = new();
        
        public GameObject row;

        public override int GetItemCount() => _list?.Count ?? 0;

        public override void OnBindViewHolder(SampleVh viewHolder, int i) => viewHolder.Bind(_list[i]);

        public override GameObject OnCreateViewHolder() => Instantiate(row);

        public void SubmitList(IEnumerable<SampleData> eventData)
        {
            _list.Clear();
            _list.AddRange(eventData);
            NotifyDatasetChanged();
        }
    }
}