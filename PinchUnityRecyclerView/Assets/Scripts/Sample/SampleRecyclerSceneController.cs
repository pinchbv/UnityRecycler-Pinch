using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sample
{
    public class SampleRecyclerSceneController : MonoBehaviour
    {
        [SerializeField] private SampleRecyclerView sampleRecycler;
        [SerializeField] private Text itemCountText;
        [SerializeField] private Button eventsToggleButton;

        private readonly List<SampleData> _sampleData = new();
        private bool _keepAddingItems = true;

        private void Start()
        {
            InitSampleData();
            InitButtons();
            InvokeRepeating(nameof(OnUpdateRecycler), 1f, 1f);
        }

        private void InitButtons()
        {
            eventsToggleButton.onClick.AddListener(() =>
                {
                    _keepAddingItems = !_keepAddingItems;
                }
            );
        }

        /// <summary>
        /// Add some initial SampleData
        /// </summary>
        private void InitSampleData()
        {
            _sampleData.Add(CreateRandomSampleData(_sampleData.Count));
            _sampleData.Add(CreateRandomSampleData(_sampleData.Count));
            _sampleData.Add(CreateRandomSampleData(_sampleData.Count));
            _sampleData.Add(CreateRandomSampleData(_sampleData.Count));
            _sampleData.Add(CreateRandomSampleData(_sampleData.Count));
        }

        private void OnUpdateRecycler()
        {
            itemCountText.text = $"Count: {_sampleData.Count}";
            if (!_keepAddingItems) return;
            _sampleData.Add(CreateRandomSampleData(_sampleData.Count));
            sampleRecycler.SubmitList(_sampleData);
        }

        private static SampleData CreateRandomSampleData(int index)
        {
            return new SampleData(
                name: System.Guid.NewGuid().ToString(),
                description: System.Guid.NewGuid().ToString(),
                code: index
            );
        }
    }
}