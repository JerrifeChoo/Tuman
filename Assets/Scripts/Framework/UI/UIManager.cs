using System.Collections.Generic;
using UnityEngine;

namespace TT.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        private Transform Root;
        private Dictionary<int, int> sortingOrder = new Dictionary<int, int>();
        private Dictionary<int, List<View>> views = new Dictionary<int, List<View>>();

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var gameObject = new GameObject("UIManager");
                    gameObject.AddComponent<UIManager>();
                    DontDestroyOnLoad(gameObject);
                }
                return instance;
            }
        }

        private void Awake()
        {
            var layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name != "Default")
                {
                    sortingOrder.Add(layers[i].id, -1);
                    views.Add(layers[i].id, new List<View>());
                }
            }
        }

        private bool Sorting(View view)
        {
            var layer = view.SortingLayer;
            views.TryGetValue(layer, out var layerViews);
            if (layerViews == null)
            {
                Destroy(view.gameObject);
                return false;
            }
            if (layer == SortingLayer.NameToID("View"))
            {
                var lastIndex = layerViews.Count - 1;
                if(lastIndex > -1)
                    layerViews[lastIndex].canvas[0].enabled = false;
            }
            else
            {
                sortingOrder.TryGetValue(layer, out var order);
                view.SortingOrder = ++order;
                var gap = view.SortingOrder - view.OrginalOrder;
                var orderDepth = 0;
                for (int i = 0; i < view.canvas.Length; i++)
                {
                    if (view.canvas[i].overrideSorting && view.canvas[i].sortingLayerID == layer)
                    {
                        view.canvas[i].sortingOrder += gap;
                        if (i != 0)
                        {
                            orderDepth = view.canvas[i].sortingOrder - view.SortingOrder;
                            if (orderDepth > view.OrderDepth)
                                view.OrderDepth = orderDepth;
                        }
                    }
                }
                sortingOrder[layer] = view.OrderDepth + order;
            }
            view.canvas[0].enabled = true;
            layerViews.Add(view);
            return true;
        }

        public View Open(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            var gameObject = Instantiate(prefab);
            var view = gameObject.GetComponent<View>();
            if (Sorting(view))
            {
                gameObject.transform.SetParent(Root);
                return view;
            }
            return null;
        }

        public void Close(View view)
        {
            var layer = view.SortingLayer;
            views.TryGetValue(layer, out var layerViews);
            if (layerViews != null)
            {
                Destroy(view.gameObject);
                return;
            }
            layerViews.Remove(view);
            Destroy(view.gameObject);
            //退还占用order
            if (layerViews.Count == 0)
                sortingOrder[layer] = -1;
            else
            {
                sortingOrder.TryGetValue(layer, out var order);
                if (order == view.SortingOrder)
                    sortingOrder[layer] -= view.OrderDepth;
            }
            if (layer == SortingLayer.NameToID("View"))
            {
                var lastIndex = layerViews.Count - 1;
                if (lastIndex > -1)
                    layerViews[lastIndex].canvas[0].enabled = true;
            }
            else
            {
            
            }
        }

        public void CloseAll()
        {
        }
    }
}
