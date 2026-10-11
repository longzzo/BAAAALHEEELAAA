namespace Valhalla.Campaign
{
    // Read-only presentation model: progress is recorded by real successful actions and saved in the profile.
    public sealed class VillageGuide
    {
        public readonly int step;
        public readonly string title, instruction;
        public readonly float? targetX;
        VillageGuide(int step,string title,string instruction,float? targetX)
        { this.step=step;this.title=title;this.instruction=instruction;this.targetX=targetX; }

        public static VillageGuide Current(PlayerProfile p)
        {
            if(!p.tutorialComplete)return new VillageGuide(1,"몸으로 배우는 첫 전투","훈련장에서 천둥 문신·강공격·패링·대시와 자동 부상을 배워보세요.",12);
            if(!p.hasViewedInventory)return new VillageGuide(2,"받은 재료 확인","I로 소지품을 여세요. 훈련 보상으로 가죽·뼈·철을 각각 4개 받았습니다.",null);
            if(!p.hasProcessed)return new VillageGuide(3,"부산물을 쓸모 있는 재료로","대장간에서 가죽·뼈·철 중 하나를 가공하세요. 재료가 없으면 먼저 출정해 모아도 됩니다.",-14);
            if(!p.hasSold)return new VillageGuide(4,"다음 출정의 자금","상인에게 물품 1개를 팔아보세요. 판매 수량과 총액을 확인한 뒤 확정합니다.",0);
            if(!p.hasReviewedTattoos)return new VillageGuide(5,"문신으로 싸움법 준비","문신소의 7개 부위 탭에서 15종의 발동 조건을 살펴보세요. 오른팔은 강공격·패링, 오른다리는 대시 문신입니다. 면적 안에서 장착한 능력만 발동합니다.",-7);
            if(!p.hasReviewedEquipment)return new VillageGuide(6,"도끼와 몸을 준비","장비 상점에서 무기와 보호대를 확인하세요. 보호대는 해당 부위 문신을 더 오래 지켜줍니다.",7);
            if(!p.hasDeparted)return new VillageGuide(7,"첫 출정","오른쪽 관문에서 E. 적을 쓰러뜨리고 재료를 모은 뒤 전진 또는 안전 귀환을 선택하세요.",18);
            return null;
        }
    }
}
