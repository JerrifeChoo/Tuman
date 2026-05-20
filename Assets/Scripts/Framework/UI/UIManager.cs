using System.Collections.Generic;
using System.Linq;
using TT.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TT.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        private Transform uiRoot;
        private List<GameObject> roots = new List<GameObject>();
        private Dictionary<int, int> sortingOrders = new Dictionary<int, int>();
        private Dictionary<int, List<View>> views = new Dictionary<int, List<View>>();
        private int UILayer;

        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var gameObject = new GameObject("UIManager");
                    instance = gameObject.AddComponent<UIManager>();
                    DontDestroyOnLoad(gameObject);
                }
                return instance;
            }
        }

        private void Awake()
        {
            var layers = SortingLayer.layers;
            UILayer = LayerMask.NameToLayer("UI");
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name != "Default")
                {
                    sortingOrders.Add(layers[i].id, -1);
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
                if (lastIndex > -1)
                    layerViews[lastIndex].canvas[0].enabled = false;
            }
            else
            {
                sortingOrders.TryGetValue(layer, out var order);
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
                sortingOrders[layer] = view.OrderDepth + order;
                if (view.OnlyVisibleOne && layerViews.Count > 0)
                {
                    foreach (var uiView in layerViews)
                    {
                        uiView.canvas[0].enabled = false;
                    }
                }
            }
            view.canvas[0].enabled = true;
            layerViews.Add(view);
            return true;
        }

        private void ResetRecord()
        {
            foreach (var key in sortingOrders.Keys.ToList())
            {
                sortingOrders[key] = -1;
            }
            foreach (var key in views.Keys.ToList())
            {
                views[key].Clear();
            }
        }

        private Transform TryGetRoot()
        {
            if (uiRoot != null)
                return uiRoot;
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(roots);
            foreach (var root in roots)
            {
                var canvas = root.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    if (canvas.gameObject.layer != UILayer)
                        continue;
                    uiRoot = canvas.transform;
                    break;
                }
            }
            ResetRecord();
            return uiRoot;
        }

        public View Open(string path)
        {
            var root = TryGetRoot();
            if (root == null) return null ;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            var gameObject = Instantiate(prefab);
            var view = gameObject.GetComponent<View>();
            if (Sorting(view))
            {
                gameObject.transform.SetParent(root, false);
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
                sortingOrders[layer] = -1;
            else
            {
                sortingOrders.TryGetValue(layer, out var order);
                if (order == view.SortingOrder)
                    sortingOrders[layer] -= view.OrderDepth;
                if (layer == SortingLayer.NameToID("View"))
                {
                    layerViews[layerViews.Count - 1].canvas[0].enabled = true;
                }
                else
                {
                    for (var i = layerViews.Count - 1; i > -1; i--)
                    {
                        layerViews[i].canvas[0].enabled = true;
                        if (layerViews[i].OnlyVisibleOne)
                            break;
                    }
                }
            }
        }

        public void CloseAll(int layerID = -1)
        {
            if (uiRoot == null)
                return;
            if (layerID == -1)
            {
                uiRoot.DestroyAllChild();
                ResetRecord();
            }
            else
            {
                views.TryGetValue(layerID, out var layerViews);
                if (layerViews == null)
                {
                    return;
                }
                sortingOrders[layerID] = -1;
                foreach (var view in layerViews)
                {
                    Destroy(view.gameObject);
                }
                views[layerID].Clear();
            }
        }
    }
}
