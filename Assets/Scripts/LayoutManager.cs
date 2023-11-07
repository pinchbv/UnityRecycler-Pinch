using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pinch.RecyclerView
{
    public class LayoutManager<T>
    {
        private Vector2 _rowDimension;
        private Vector2 _rowScale;
        private ScrollRect _scrollRect;
        private RectTransform SelfRectTransform { get; set; }
        private RectTransform GridRectTransform { get; set; }
        private GameObject _grid;
        private float _limitBottom;
        public bool IsCreating;
        [UsedImplicitly] private bool _isDragging;
        private bool _isClickDown;

        private readonly RecyclerView<T> _recyclerView;

        public LayoutManager(RecyclerView<T> recyclerView)
        {
            _recyclerView = recyclerView;
        }

        public int GetFirstPosition() => _recyclerView.isReverse
            ? Mathf.Abs(Mathf.RoundToInt(
                Mathf.Clamp(_grid.transform.GetComponent<RectTransform>().offsetMin.y, -_limitBottom, 0) /
                (GetRowSize().y)))
            : Mathf.RoundToInt(Mathf.Clamp(_grid.transform.GetComponent<RectTransform>().offsetMin.y, 0,
                _limitBottom) / (GetRowSize().y));

        public int GetScreenListSize() => Mathf.FloorToInt(Screen.height / GetRowSize().y);

        public void Create()
        {
            SelfRectTransform = _recyclerView.GetComponent<RectTransform>();
            _grid = new GameObject
            {
                name = "Grid"
            };
            GridRectTransform = _grid.AddComponent<RectTransform>();
            GridRectTransform.sizeDelta = Vector2.zero;

            if (_recyclerView.isReverse)
            {
                GridRectTransform.anchorMax = new Vector2(0.5f, 0f);
                GridRectTransform.anchorMin = new Vector2(0.5f, 0f);
                GridRectTransform.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                GridRectTransform.anchorMax = new Vector2(0.5f, 1f);
                GridRectTransform.anchorMin = new Vector2(0.5f, 1f);
                GridRectTransform.pivot = new Vector2(0.5f, 1f);
            }


            _grid.transform.SetParent(_recyclerView.transform);
            GridRectTransform.anchoredPosition = Vector3.zero;

            _scrollRect = _recyclerView.GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                _scrollRect = _recyclerView.gameObject.AddComponent<ScrollRect>();
            }

            _scrollRect.content = GridRectTransform;
            _scrollRect.onValueChanged.AddListener(delegate { OnScroll(); });
            _scrollRect.viewport = SelfRectTransform;
            _scrollRect.content = GridRectTransform;
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.inertia = true;
            _scrollRect.decelerationRate = _recyclerView.decelerationRate;
            _scrollRect.scrollSensitivity = 10f;
            _scrollRect.vertical = true;
            _scrollRect.horizontal = false;

            if (_recyclerView.GetComponent<Image>() == null)
            {
                var image = _recyclerView.gameObject.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0.01f);
            }

            if (_recyclerView.GetComponent<Mask>() == null)
            {
                _recyclerView.gameObject.AddComponent<Mask>();
            }

            if (_recyclerView.gameObject.GetComponent<EventTrigger>() != null) return;

            var eventTrigger = _recyclerView.gameObject.AddComponent<EventTrigger>();
            var pointUp = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            pointUp.callback.AddListener(_ => OnClickUp());
            eventTrigger.triggers.Add(pointUp);

            var pointDown = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            pointDown.callback.AddListener(_ => OnClickDown());
            eventTrigger.triggers.Add(pointDown);

            var drag = new EventTrigger.Entry
            {
                eventID = EventTriggerType.Drag
            };
            drag.callback.AddListener(_ => OnDrag());
            eventTrigger.triggers.Add(drag);
        }

        private void OnDrag()
        {
            _isDragging = true;
        }

        private void OnClickDown()
        {
            _isClickDown = true;
        }

        private void OnClickUp()
        {
            _isDragging = false;
            _isClickDown = false;
        }

        private Vector2 GetRowSize()
        {
            return new Vector2(0, ((_rowDimension.y * _rowScale.y) + _recyclerView.spacing.y));
        }

        public void SetPositionViewHolder(IViewHolderInfo vh)
        {
            var size = GetRowSize();

            vh.RectTransform.localPosition = _recyclerView.isReverse
                ? new Vector3(0, vh.CurrentIndex * size.y, 0)
                : new Vector3(0, -vh.CurrentIndex * size.y, 0);
        }

        private void Invalidate()
        {
            if (_recyclerView.isReverse)
            {
                if (GridRectTransform.offsetMax.y < -_limitBottom)
                    _recyclerView.OnDataChange(_recyclerView.GetItemCount() - 1);
                else
                    _recyclerView.OnDataChange();
            }
            else
            {
                if (GridRectTransform.offsetMax.y > _limitBottom)
                    _recyclerView.OnDataChange(_recyclerView.GetItemCount() - 1);
                else
                    _recyclerView.OnDataChange();
            }
        }

        private void OnScroll()
        {
            if (IsCreating) return;

            if (!IsStateValid()) Invalidate();
            else
            {
                _recyclerView.UpdateScrap();
                ClampList();
            }
        }

        public void OnDataChange(GameObject initialVh, int pos = 0)
        {
            _rowDimension = new Vector2(initialVh.GetComponent<RectTransform>().rect.width,
                initialVh.GetComponent<RectTransform>().rect.height);
            _rowScale = initialVh.GetComponent<RectTransform>().localScale;
            var rowSize = GetRowSize();

            _limitBottom = _recyclerView.GetItemCount() * rowSize.y - SelfRectTransform.rect.height -
                           _recyclerView.spacing.y;

            if (_recyclerView.isReverse)
            {
                GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, -rowSize.y * pos);
                GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
            }
            else
            {
                GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, rowSize.y * pos);
                GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
            }
        }

        private IEnumerator ScrollToAdapterPosition(Vector2 dir, float speed = 50)
        {
            _scrollRect.inertia = false;

            var v = new Vector2(0, dir.y * _limitBottom);
            var offsetMax = GridRectTransform.offsetMax;
            var goUp = offsetMax.y > v.y;
            var y = offsetMax.y;
            while (goUp ? GridRectTransform.offsetMax.y > v.y : GridRectTransform.offsetMax.y < v.y)
            {
                y += goUp ? -speed : speed;
                if (y > _limitBottom)
                {
                    y = _limitBottom;
                    GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, y);
                    GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                    OnScroll();
                    break;
                }

                GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, y);
                GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                OnScroll();
                yield return new WaitForEndOfFrame();
                if (_isClickDown) break;
            }

            _scrollRect.inertia = true;
        }

        public void ScrollTo(Vector2 pos) => _recyclerView.StartCoroutine(ScrollToAdapterPosition(pos));

        public void ScrollTo(int position) => _recyclerView.StartCoroutine(NotifyDatasetChanged(position));

        public void SmoothScrollTo(int position) =>
            _recyclerView.StartCoroutine(
                ScrollToAdapterPosition(
                    new Vector2(0, (GetRowSize().y * position) / _limitBottom))
            );

        private IEnumerator NotifyDatasetChanged(int pos = 0)
        {
            _scrollRect.inertia = false;
            _recyclerView.OnDataChange(pos);
            yield return new WaitForEndOfFrame();
            OnScroll();
            _scrollRect.inertia = true;
        }

        public void AttachToGrid(IViewHolderInfo vh, bool up)
        {
            vh.ItemView.transform.SetParent(_grid.transform);
            if (up)
            {
                vh.ItemView.transform.SetAsLastSibling();
            }
            else
            {
                vh.ItemView.transform.SetAsFirstSibling();
            }

            vh.ItemView.name = vh.CurrentIndex.ToString();
            vh.ItemView.SetActive(true);
            SetPivot(vh.RectTransform);
            SetPositionViewHolder(vh);
        }

        private bool IsStateValid()
        {
            if (_recyclerView.GetItemCount() == 0)
            {
                return true;
            }

            return _recyclerView.AttachedScrap.Any(vh => !vh.IsHidden());
        }

        public void Clear()
        {
            foreach (Transform row in _grid.transform)
            {
                Object.Destroy(row.gameObject);
            }
        }

        private void SetPivot(RectTransform rect)
        {
            rect.pivot = _recyclerView.isReverse ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        }

        public void ClampList()
        {
            if (_recyclerView.isReverse)
            {
                if (GridRectTransform.offsetMax.y > 0)
                {
                    GridRectTransform.localPosition = new Vector2(GridRectTransform.localPosition.x, 0);
                    GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, 0);
                    GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                }
                else if (GridRectTransform.offsetMax.y < -_limitBottom)
                {
                    GridRectTransform.localPosition =
                        new Vector2(GridRectTransform.localPosition.x, -_limitBottom);
                    GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, -_limitBottom);
                    GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                }
            }
            else
            {
                if (GridRectTransform.offsetMax.y < 0)
                {
                    GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, 0);
                    GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                }
                else if (GridRectTransform.offsetMax.y > _limitBottom)
                {
                    GridRectTransform.offsetMax = new Vector2(GridRectTransform.offsetMax.x, _limitBottom);
                    GridRectTransform.sizeDelta = new Vector2(GridRectTransform.sizeDelta.x, 0);
                }
            }
        }
    }
}