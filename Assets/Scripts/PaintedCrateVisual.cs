using UnityEngine;

namespace Valhalla.Campaign
{
    // View only: parent retains breaking, carrying, flight and pressure-plate behavior.
    public sealed class PaintedCrateVisual : MonoBehaviour
    {
        Sprite ownedSprite;
        public static bool Attach(Transform parent,float height,int order)
        {
            var texture=Resources.Load<Texture2D>("Art/SupplyCrate-v1");
            if(!texture)return false;
            var child=new GameObject("Painted supply crate");child.transform.SetParent(parent,false);
            var view=child.AddComponent<PaintedCrateVisual>();
            float sx=texture.width/1254f,sy=texture.height/1254f;
            var rect=new Rect(85*sx,103*sy,1085*sx,1051*sy);
            view.ownedSprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),rect.height/height);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sprite=view.ownedSprite;renderer.sortingOrder=order;
            return true;
        }
        void OnDestroy() { if(ownedSprite)Destroy(ownedSprite); }
    }
}
