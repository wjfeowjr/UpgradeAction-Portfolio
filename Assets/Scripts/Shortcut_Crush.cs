using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public  class Shortcut_Crush : ShortcutObject, IHitProduct
{
    [SerializeField] private int targetIdx;
    [SerializeField] private TileFactory tileFactory;
    
    private Room targetRoom;

    public Room TargetRoom
    {
        get => targetRoom;
        set => targetRoom = value;
    }

    public override void OpenProduct()
    {
        tileFactory.Crash();
        base.OpenProduct();
    }

    protected override void OpenImmediate()
    {
        base.OpenImmediate();
        if(targetRoom)
            targetRoom.ShortcutOpen(targetRoom.GetWallShortCutName(targetIdx));
    }

    public void HitProduct()
    {
        tileFactory.HitProduct();
    }
}