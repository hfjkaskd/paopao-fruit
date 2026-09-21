public sealed class GNEJJHEDEBL : FOLJNEPEKCA<GNEJJHEDEBL> {
    public int GetItemUnlockLevel(POJCEPBNNIP type) {
        var config = PropConfigSO.Instance.GetPropConfigInfo(HarvestBridge.MapProp(type));
        return config != null && config.unlockFunction ? config.unlockCondition.unlockLevel : 1;
    }
}
