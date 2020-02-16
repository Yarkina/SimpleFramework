// using System;
// using UnityEngine;

// public class BaseUI : MonoBehaviour
// {
//     public int Order
//     {
//         get
//         {
//             return order;
//         }

//         set
//         {
//             order = value;
//         }
//     }

//     public bool IsCreate
//     {
//         get
//         {
//             return isCreate;
//         }

//         set
//         {
//             isCreate = value;
//         }
//     }

//     public bool IsOpen
//     {
//         get
//         {
//             return isOpen;
//         }
//         set
//         {
//             isOpen = value;
//         }
//     }
//     public void Open()
//     {
//         if (!isCreate)
//         {
//             isCreate = true;
//             OnCreate();
//             Create();
//         }
//         UIManager.Instance.CloseByOrder(order, StartOpen);
//     }

//     public void Close(Action callBack = null)
//     {
//         isOpen = false;
//         OnClose(callBack);
//     }
//     //创建，只执行一次
//     public virtual void OnCreate() { }
//     //打开UI
//     public virtual void OnOpen() { }
//     //退出UI
//     public virtual void OnClose(Action callBack = null)
//     {
//         gameObject.SetActive(false);
//         if (callBack != null)
//         {
//             callBack();
//         }
//     }
//     private void StartOpen()
//     {
//         gameObject.SetActive(true);
//         isOpen = true;
//         OnOpen();
//     }
//     protected void Create()
//     {
//         RectTransform parent = UIManager.Instance.GetOrderRoot(order);
//         var rect = transform.GetComponent<RectTransform>();
//         rect.SetParent(parent);
//         ResourceManager.Identify(transform.gameObject);
//         rect.offsetMin = new Vector2(0, 0);
//         rect.offsetMax = new Vector2(0, 0);
//         rect.anchorMin = new Vector2(0, 0);
//         rect.anchorMax = new Vector2(1, 1);
//     }
//     private int order;
//     protected string id;
//     protected bool isCreate = false; //是否创建
//     protected bool isOpen = false; //是否打开
// }