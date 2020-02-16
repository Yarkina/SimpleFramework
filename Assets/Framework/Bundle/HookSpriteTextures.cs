using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AB
{
    //修复AB.unload卸载 deactive sprite的贴图BUG 
    public class HookSpriteTextures : MonoBehaviour
    {
        List<Texture> textures = new List<Texture>();

        void Awake()
        {
            
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                var sp = img.sprite;
                if (sp != null)
                {
                    textures.Add(sp.texture);
                }
            }
        }

        void OnDestroy()
        {
            textures.Clear();
        }
    }
}
