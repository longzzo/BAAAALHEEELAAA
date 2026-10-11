using System.Collections.Generic;
using UnityEngine;

namespace Valhalla.Campaign
{
    // Replaceable view only. Hit boxes, weapon timing and injury rules remain in Fighters.
    public sealed class PaintedCombatVisual : MonoBehaviour
    {
        SpriteRenderer painting;
        Barbarian hero;
        Enemy enemy;
        Vector3 previousPosition;
        float stride;
        Color tint;
        readonly List<Sprite> ownedSprites = new List<Sprite>();
        Sprite originalSprite;
        Sprite[] walkFrames;
        bool hasWalkCycle;
        bool walking;
        float walkAnchorX;
        float walkDistance;
        int walkFrame = -1;
        Transform axePivot;
        Sprite[] heavyFrames;
        bool hasHeavyCycle;
        int heavyFrame = -1;
        Sprite[] enemyAttackFrames;
        bool hasEnemyAttackCycle;
        int enemyAttackFrame = -1;
        public bool HasWalkCycle => hasWalkCycle;
        public int WalkFrame => walkFrame;
        public bool HasHeavyCycle => hasHeavyCycle;
        public int HeavyFrame => heavyFrame;
        public bool HasEnemyAttackCycle => hasEnemyAttackCycle;
        public int EnemyAttackFrame => enemyAttackFrame;
        public void Initialize(bool hostile)
        {
            hero=GetComponent<Barbarian>();enemy=GetComponent<Enemy>();
            var visual=transform.Find("Visual");if(!visual)return;
            var texture=Resources.Load<Texture2D>(hostile?"Art/Draugr-v1":"Art/BarbarianConcept-v1");if(!texture)return;
            foreach(Transform child in visual)
                if(child.name!="Axe pivot"&&child.name!="Crown"&&child.name!="Antler")child.gameObject.SetActive(false);
            var o=new GameObject("Painted fighter");o.transform.SetParent(visual,false);o.transform.localPosition=new Vector3(0,-1.06f);
            painting=o.AddComponent<SpriteRenderer>();
            painting.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.015f),texture.height/2.18f);
            originalSprite=painting.sprite;ownedSprites.Add(originalSprite);
            painting.sortingOrder=20;
            tint=hostile?(enemy&&enemy.charger?new Color(1,.8f,.68f):enemy&&enemy.elite?new Color(.65f,.8f,.92f):Color.white):Color.white;
            painting.color=tint;previousPosition=transform.position;
            var axe=Resources.Load<Texture2D>("Art/Axe-v1");var pivot=visual.Find("Axe pivot");
            axePivot=pivot;
            if(axe&&pivot)
            {
                foreach(Transform child in pivot)child.gameObject.SetActive(false);
                var blade=new GameObject("Painted axe");blade.transform.SetParent(pivot,false);
                blade.transform.localPosition=new Vector3(.1f,0);blade.transform.localRotation=Quaternion.Euler(0,0,-20);
                var sr=blade.AddComponent<SpriteRenderer>();sr.sortingOrder=28;
                sr.sprite=Sprite.Create(axe,new Rect(0,0,axe.width,axe.height),new Vector2(.445f,.35f),axe.height/1.85f);
                ownedSprites.Add(sr.sprite);
            }
            if(hero)
            {
                var walkTexture=Resources.Load<Texture2D>("Art/BarbarianWalk-v1");
                if(walkTexture)
                {
                    float cellWidth=walkTexture.width/3f;
                    float cellHeight=walkTexture.height/2f;
                    // Measured opaque foot baselines in the approved 512px cells, top-origin.
                    // Keep one scale across the cycle; only the foot pivot varies to remove sheet-layout jitter.
                    int[] footY={498,501,500,479,482,479};
                    float ppu=cellHeight*(454f/512f)/2.18f;
                    walkFrames=new Sprite[6];
                    for(int i=0;i<6;i++)
                    {
                        int col=i%3;int row=i<3?1:0;
                        var frame=Sprite.Create(walkTexture,new Rect(col*cellWidth,row*cellHeight,cellWidth,cellHeight),new Vector2(.5f,1-footY[i]/512f),ppu);
                        walkFrames[i]=frame;ownedSprites.Add(frame);
                    }
                    hasWalkCycle=true;
                }
                var heavyTexture=Resources.Load<Texture2D>("Art/BarbarianHeavy-v1");
                if(heavyTexture)
                {
                    // Calibrated rectangles include each complete axe while excluding the neighboring pose.
                    // The generated lower-row axe tips slightly cross the nominal 512px grid.
                    float scaleX=heavyTexture.width/1536f,scaleY=heavyTexture.height/1024f;
                    Rect[] rects={new Rect(60,512,416,512),new Rect(572,512,416,512),new Rect(1103,512,382,512),new Rect(49,0,480,512),new Rect(585,0,452,512),new Rect(1085,0,426,512)};
                    float[] anchorX={300,825,1310,240,770,1255};
                    float[] footY={490,490,490,438,438,438};
                    float ppu=356f*scaleY/2.18f;
                    heavyFrames=new Sprite[6];
                    for(int i=0;i<6;i++)
                    {
                        var r=rects[i];var pivotPoint=new Vector2((anchorX[i]-r.x)/r.width,1-footY[i]/512f);
                        var frame=Sprite.Create(heavyTexture,new Rect(r.x*scaleX,r.y*scaleY,r.width*scaleX,r.height*scaleY),pivotPoint,ppu);
                        heavyFrames[i]=frame;ownedSprites.Add(frame);
                    }
                    hasHeavyCycle=true;
                }
            }
            if(enemy)
            {
                // Root generates this atlas; initial equal 512px cells until calibrated like the heavy sheet.
                var attackTexture=Resources.Load<Texture2D>("Art/DraugrAttack-v1");
                if(attackTexture)
                {
                    float sx=attackTexture.width/1536f,sy=attackTexture.height/1024f;
                    // Lower-row axe overhangs the nominal grid; isolate complete silhouettes and align feet.
                    Rect[] rects={new Rect(0,512,512,512),new Rect(512,512,512,512),new Rect(1024,512,512,512),new Rect(35,0,505,512),new Rect(562,0,478,512),new Rect(1060,0,476,512)};
                    float[] anchorX={250,768,1280,250,768,1280};
                    float[] footY={489,489,490,439,439,439};
                    float ppu=360f*sy/2.18f;
                    enemyAttackFrames=new Sprite[6];
                    for(int i=0;i<6;i++)
                    {
                        var r=rects[i];
                        var frame=Sprite.Create(attackTexture,new Rect(r.x*sx,r.y*sy,r.width*sx,r.height*sy),new Vector2((anchorX[i]-r.x)/r.width,1-footY[i]/512f),ppu);
                        enemyAttackFrames[i]=frame;ownedSprites.Add(frame);
                    }
                    hasEnemyAttackCycle=true;
                }
            }
        }
        void OnDisable()
        {
            if(axePivot)axePivot.gameObject.SetActive(true);
            if(painting&&heavyFrame>=0)painting.sprite=originalSprite;
            if(painting&&enemyAttackFrame>=0)painting.sprite=originalSprite;
            heavyFrame=-1;enemyAttackFrame=-1;
        }
        void OnDestroy()
        {
            foreach(var sprite in ownedSprites)if(sprite)Destroy(sprite);
        }
        bool HeavyActive => hasHeavyCycle&&hero&&hero.Attacking&&hero.heavy&&Game.I.phase==Phase.Combat;
        bool EnemyAttackActive => hasEnemyAttackCycle&&enemy&&enemy.Alive&&!enemy.Captured&&enemy.stun<=0&&Game.I.phase==Phase.Combat&&(enemy.state==1||enemy.state==2||enemy.state==3);
        void LateUpdate()
        {
            if(!painting||!Game.I)return;
            // Manual pause/choice holds the visual exactly; nothing advances, not even the heavy pose.
            if(Game.I.paused||Game.I.choice.HasValue){previousPosition=transform.position;walkAnchorX=transform.position.x;return;}
            if(Game.I.Frozen)
            {
                // Hitstop from this frame's impact must still land on the impact pose before holding there.
                previousPosition=transform.position;walkAnchorX=transform.position.x;
                UpdateHeavyCycle();
                UpdateEnemyAttackCycle();
                return;
            }
            float movement=Mathf.Abs(transform.position.x-previousPosition.x);previousPosition=transform.position;
            stride+=movement*2.7f;
            bool attacking=hero?hero.Attacking:enemy&&enemy.state==2;
            bool heavyActive=HeavyActive;
            float lean=heavyActive?0:enemy&&enemy.Captured?-40:hero&&hero.Holding?5:hero&&hero.Headbutting?-18:attacking?-6:movement>.003f?-2:0;
            painting.transform.localRotation=Quaternion.Euler(0,0,lean);
            painting.transform.localScale=heavyActive?Vector3.one:new Vector3(1,1+(movement>.003f?Mathf.Sin(stride)*.015f:0),1);
            var color=tint;
            if(hero&&hero.invulnerability>0&&Mathf.FloorToInt(Time.time*20)%2==0)color.a=.45f;
            painting.color=color;
            UpdateWalkCycle();
            UpdateHeavyCycle();
            UpdateEnemyAttackCycle();
        }
        void UpdateWalkCycle()
        {
            if(!hasWalkCycle||!hero)return;
            bool canWalk=hero.Grounded&&!hero.Attacking&&!hero.Parrying&&!hero.Dodging&&!hero.Holding&&!hero.Headbutting&&Mathf.Abs(hero.velocity.x)>.1f;
            if(!canWalk)
            {
                if(walking){walking=false;walkFrame=-1;painting.sprite=originalSprite;}
                return;
            }
            float x=transform.position.x;
            if(!walking){walking=true;walkAnchorX=x;walkDistance=0;}
            float frameMove=x-walkAnchorX;walkAnchorX=x;
            if(Mathf.Abs(frameMove)>2f){walkDistance=0;walkFrame=0;painting.sprite=walkFrames[0];return;}
            walkDistance+=frameMove*hero.facing;
            float cyclePos=walkDistance/2f;cyclePos-=Mathf.Floor(cyclePos);
            walkFrame=Mathf.Clamp(Mathf.FloorToInt(cyclePos*6f),0,5);
            painting.sprite=walkFrames[walkFrame];
        }
        void UpdateHeavyCycle()
        {
            if(axePivot&&hero)axePivot.gameObject.SetActive(!HeavyActive&&!hero.CarryingProp);
            if(!hasHeavyCycle)return;
            bool heavyActive=HeavyActive;
            // Generated art already draws the weapon in-hand; the independent axe pivot must hide while it plays.
            if(!heavyActive){if(heavyFrame>=0&&!walking)painting.sprite=originalSprite;heavyFrame=-1;return;}
            float elapsed=hero.AttackElapsed,recovery=hero.AttackRecoveryProgress;
            heavyFrame=elapsed<.14f?0:elapsed<.30f?1:elapsed<.43f?2:recovery<.2f?3:recovery<.55f?4:5;
            painting.sprite=heavyFrames[heavyFrame];
            painting.transform.localRotation=Quaternion.identity;
            painting.transform.localScale=Vector3.one;
        }
        void UpdateEnemyAttackCycle()
        {
            // Missing atlas falls back untouched to the existing cutout pose; only gate the axe once the sheet is active.
            if(!hasEnemyAttackCycle)return;
            bool active=EnemyAttackActive;
            if(axePivot)axePivot.gameObject.SetActive(!active);
            if(!active){if(enemyAttackFrame>=0)painting.sprite=originalSprite;enemyAttackFrame=-1;return;}
            if(enemy.state==1)
            {
                float windup=enemy.elite?1.05f:enemy.boss?1f:enemy.charger?.95f:.8f;
                float progress=Mathf.Clamp01((windup-enemy.timer)/windup);
                enemyAttackFrame=progress<1f/3f?0:progress<2f/3f?1:2;
            }
            else if(enemy.state==2) enemyAttackFrame=3;
            else
            {
                float duration=enemy.boss?1.2f:.95f;
                float progress=Mathf.Clamp01((duration-enemy.timer)/duration);
                enemyAttackFrame=progress<.5f?4:5;
            }
            painting.sprite=enemyAttackFrames[enemyAttackFrame];
            painting.transform.localRotation=Quaternion.identity;
            painting.transform.localScale=Vector3.one;
        }
    }
}
