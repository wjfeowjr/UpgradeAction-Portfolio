using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;

// 투명벽: 겉보기에는 일반 벽이지만, 플레이어가 닿아 있는 동안 알파값이 내려가
// 뒤에 숨겨진 공간이 비쳐 보이고, 빠져나오면 다시 채워져 벽처럼 보인다.
// 순수 연출 전용 컴포넌트. 발견(사운드/미니맵 공개/저장)은 HiddenArea의 areaCollider 진입이 담당한다.
public class TransparentWall : MonoBehaviour
{
    [Header("접촉 판정")]
    [SerializeField] private TilemapCollider2D touchCollider;

    [Header("페이드시킬 벽 타일맵")]
    [SerializeField] private Tilemap[] wallTilemaps;

    [Header("드러남 연출")]
    [SerializeField] private float revealAlpha = 0.5f;    // 닿아 있는 동안의 알파값
    [SerializeField] private float fadeDuration = 0.3f;   // 페이드 시간

    private bool isTouching;
    private float curAlpha = 1f;
    private Tween fadeTween;
    private List<List<Vector3Int>> tileCellsList = new List<List<Vector3Int>>();

    public bool IsTouching => isTouching;

    // Room.Awake에서 호출: 타일 좌표를 캐싱하고 색상 잠금을 해제한다
    public void Init()
    {
        if (touchCollider)
            touchCollider.isTrigger = true;

        foreach (var tilemap in wallTilemaps)
        {
            var cells = new List<Vector3Int>();
            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(pos))
                    continue;

                // 기본 타일 에셋은 색상이 잠겨 있어 SetColor가 무시되므로 잠금을 해제한다
                tilemap.SetTileFlags(pos, TileFlags.None);
                cells.Add(pos);
            }
            tileCellsList.Add(cells);
        }
    }

    // 플레이어가 닿아 있는 동안 반투명, 빠져나오면 다시 불투명으로 페이드
    public void CheckTouch(Vector2 playerPos)
    {
        if (!touchCollider)
            return;

        bool touching = touchCollider.OverlapPoint(playerPos);
        if (touching == isTouching)
            return;

        isTouching = touching;
        Fade(touching ? revealAlpha : 1f);
    }

    // 현재 알파값에서 목표 알파값으로 페이드. 진행 중인 페이드는 끊고 이어간다
    private void Fade(float targetAlpha)
    {
        fadeTween?.Kill();
        fadeTween = DOTween.To(() => curAlpha, x =>
        {
            curAlpha = x;
            ApplyAlpha(x);
        }, targetAlpha, fadeDuration);
    }

    // 캐싱된 모든 벽 타일에 알파값 적용
    private void ApplyAlpha(float alpha)
    {
        var color = new Color(1f, 1f, 1f, alpha);
        for (int i = 0; i < wallTilemaps.Length; i++)
        {
            foreach (var cell in tileCellsList[i])
                wallTilemaps[i].SetColor(cell, color);
        }
    }
}
