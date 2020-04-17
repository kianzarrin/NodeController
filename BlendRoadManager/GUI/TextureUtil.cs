using ColossalFramework.UI;
using UnityEngine;

namespace BlendRoadManager.GUI {
    public static class TextureUtil {
        public static UITextureAtlas GetAtlas(string name) {
            UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
            for (int i = 0; i < atlases.Length; i++) {
                if (atlases[i].name == name)
                    return atlases[i];
            }
            return UIView.GetAView().defaultAtlas;
        }
    }
}
