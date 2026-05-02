using System;
using UnityEngine;

namespace TT.UI
{
    [RequireComponent(typeof(Canvas))]
    public class View : MonoBehaviour
    {
        [NonSerialized]
        public int OrginalOrder;
        [NonSerialized]
        public int SortingLayer = 0;
        [NonSerialized]
        public int SortingOrder = 0;
        [NonSerialized]
        public int OrderDepth = 0;
        [NonSerialized]
        public Canvas[] canvas;

        public bool FullScreen;

        private void Awake()
        {
            canvas = GetComponentsInChildren<Canvas>();
            canvas[0].overrideSorting = true;
            OrginalOrder = canvas[0].sortingOrder;
            SortingLayer = canvas[0].sortingLayerID;
        }
    }
}
