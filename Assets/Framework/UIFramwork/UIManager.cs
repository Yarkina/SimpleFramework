// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// public class UIManager
// {
//     private static UIManager _instance;
//     private Transform canvasTransform;
//     private Transform CanvasTransform
//     {
//         get
//         {
//             if (canvasTransform == null)
//             {
//                 canvasTransform = GameObject.Find("Canvas").transform;
//             }
//             return canvasTransform;
//         }
//     }
//     public static UIManager Instance
//     {
//         get
//         {
//             if (_instance == null)
//             {
//                 _instance = new UIManager();
//             }

//             return _instance;
//         }
//     }

//     private Dictionary<string, BaseUI> panelDict;
//     private Dictionary<int, RectTransform> orderRootDict = new Dictionary<int, RectTransform>();

//     private UIManager()
//     {

//     }
//     //打开特定ui
//     public BaseUI Open(string panelType)
//     {
//         BaseUI panel = GetPanel(panelType);
//         if (panel != null)
//         {
//             panel.Open();
//             return panel;
//         }
//         return null;
//     }
//     //主动关闭某ui
//     public void Close(string panelType, Action callBack = null)
//     {
//         BaseUI panel = GetPanel(panelType);
//         if (panel.IsOpen)
//         {
//             panel.Close(callBack);
//         }
//     }
//     public void CloseByOrder(int order, Action callBack)
//     {
//         foreach (var item in panelDict.Values)
//         {
//             BaseUI ui = item;
//             if (ui.Order == order && ui.IsOpen)
//             {
//                 ui.Close(callBack);
//                 return;
//             }
//         }
//         if (callBack != null)
//         {
//             callBack();
//         }

//     }
//     public void CloseAll()
//     {
//         foreach (var item in panelDict.Values)
//         {
//             BaseUI ui = item;
//             if (ui.IsOpen)
//             {
//                 ui.Close();
//             }

//         }
//     }

//     public string GetTopUIName()
//     {
//         string uiName = null;
//         int order = 0;
//         foreach (var item in panelDict.Values)
//         {
//             BaseUI ui = item;
//             if (ui.IsOpen)
//             {
//                 if (ui.Order > order)
//                 {
//                     order = ui.Order;
//                     uiName = ui.gameObject.name;
//                 }
//             }
//         }
//         uiName = uiName.Substring(0, uiName.IndexOf('('));
//         return uiName;
//     }

//     public BaseUI GetPanel(string panelType)
//     {
//         if (panelDict == null)
//         {
//             panelDict = new Dictionary<string, BaseUI>();
//         }
//         BaseUI panel = panelDict.GetValue(panelType);
//         //如果没有实例化面板，寻找路径进行实例化，并且存储到已经实例化好的字典面板中
//         if (panel == null)
//         {
//             UIConfig uiConfig = ConfigsManager.Instance.eeDataManager.Get<UIConfig>(panelType);
//             GameObject panelGo = ResourceManager.Instance.LoadAsset<GameObject>(uiConfig.ABName, uiConfig.PrefabName);
//             panelGo = GameObject.Instantiate(panelGo, CanvasTransform, false);
//             panel = panelGo.GetComponent<BaseUI>();
//             panelDict.Add(panelType, panel);
//             panel.Order = uiConfig.Group;
//         }
//         return panel;
//     }
//     public RectTransform GetOrderRoot(int order)
//     {
//         RectTransform rect = null;
//         if (orderRootDict.TryGetValue(order, out rect))
//         {
//             return rect;
//         }
//         GameObject obj = new GameObject();
//         rect = obj.AddComponent<RectTransform>();
//         rect.SetParent(CanvasTransform.transform);
//         ResourceManager.Identify(obj);
//         rect.offsetMin = new Vector2(0, 0);
//         rect.offsetMax = new Vector2(0, 0);
//         rect.anchorMin = new Vector2(0, 0);
//         rect.anchorMax = new Vector2(1, 1);
//         obj.name = "order_" + order + "_root";
//         // var adapter = obj.AddComponent<ScreenAdapter>();
//         // adapter.Adapt();

//         orderRootDict[order] = rect;
//         ReSortSiblingIndex();
//         return rect;
//     }

//     // public void AdaptScreen()
//     // {
//     //     foreach (var p in orderRootDict)
//     //     {
//     //         var ad = p.Value.GetComponent<ScreenAdapter>();
//     //         if (ad != null)
//     //         {
//     //             ad.Adapt();
//     //         }
//     //     }
//     // }

//     private int ReSortSiblingIndex()
//     {
//         List<int> layers = new List<int>();
//         layers.AddRange(orderRootDict.Keys);
//         layers.Sort();
//         int count = layers.Count;
//         for (int i = 0; i < count; i++)
//         {
//             orderRootDict[layers[i]].SetSiblingIndex(i);
//         }
//         return 0;
//     }
// }