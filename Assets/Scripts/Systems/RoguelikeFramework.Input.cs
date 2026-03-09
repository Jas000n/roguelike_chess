using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
    #region Input

    private void HandleUnitInspectClick()
    {
        if ((state != RunState.Battle && state != RunState.Prepare) || !Input.GetMouseButtonDown(0)) return;
        if (isDragging) return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // 点击空白处关闭悬浮窗
            showTooltip = false;
            return;
        }

        foreach (var v in views)
        {
            if (v.go == hit.collider.gameObject)
            {
                inspectedUnit = v.unit;
                ShowTooltip(BuildUnitTooltip(v.unit), v.unit);
                battleLog = $"查看 {v.unit.Name}{v.unit.star}★";
                return;
            }
        }

        // 点到非棋子对象也关闭
        showTooltip = false;
    }

    private void HandleMouseDrag()
    {
        if (state != RunState.Prepare) return;

        int deployCols = 5;
        int deployRows = H;
        float s = Mathf.Max(0.01f, uiScale);
        float guiH = Screen.height / s;

        float panelX = 16f;
        float panelY = guiH - 170f;
        float benchX = panelX + 16f;
        float benchY = panelY + 88f;
        float benchSlotW = 90f;
        float benchBtnW = 84f;
        int benchCols = 8;

        int FindDeployAt(int x, int y)
        {
            for (int i = 0; i < deploySlots.Count; i++) if (deploySlots[i].x == x && deploySlots[i].y == y) return i;
            return -1;
        }

        Vector2 GetGridAtMouse()
        {
            float mx = Input.mousePosition.x / s;
            float myGui = guiH - (Input.mousePosition.y / s);

            for (int r = 0; r < deployRows; r++)
            {
                for (int c = 0; c < deployCols; c++)
                {
                    var cell = GetBoardCellGuiRect(c, r);
                    if (mx >= cell.x && mx <= cell.xMax && myGui >= cell.y && myGui <= cell.yMax) return new Vector2(c, r);
                }
            }

            for (int i = 0; i < benchCols; i++)
            {
                float bx = benchX + i * benchSlotW;
                if (mx >= bx && mx <= bx + benchBtnW && myGui >= benchY && myGui <= benchY + 45f) return new Vector2(-1, i);
            }
            return new Vector2(-2, -2);
        }

        if (!isDragging)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 clickGrid = GetGridAtMouse();
                pendingDrag = false;
                draggingDeploy = -1;
                draggingFromBench = false;

                if (clickGrid.x >= 0 && clickGrid.x < deployCols && clickGrid.y >= 0 && clickGrid.y < deployRows)
                {
                    int deployIdx = FindDeployAt((int)clickGrid.x, (int)clickGrid.y);
                    if (deployIdx >= 0)
                    {
                        draggingDeploy = deployIdx;
                        draggingFromBench = false;
                        pendingDrag = true;
                        pendingDragStart = Input.mousePosition;
                    }
                }
                else if (clickGrid.x == -1)
                {
                    int benchIdx = (int)clickGrid.y;
                    if (benchIdx >= 0 && benchIdx < benchUnits.Count)
                    {
                        draggingDeploy = benchIdx;
                        draggingFromBench = true;
                        pendingDrag = true;
                        pendingDragStart = Input.mousePosition;
                    }
                }
                return;
            }

            if (pendingDrag && Input.GetMouseButton(0))
            {
                if (Vector2.Distance((Vector2)Input.mousePosition, pendingDragStart) > 8f)
                {
                    isDragging = true;
                    pendingDrag = false;
                    if (draggingFromBench && draggingDeploy >= 0 && draggingDeploy < benchUnits.Count)
                        battleLog = $"拖拽中：{benchUnits[draggingDeploy].Name}";
                    else if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
                        battleLog = $"拖拽中：{deploySlots[draggingDeploy].Name}";
                }
                return;
            }

            if (pendingDrag && Input.GetMouseButtonUp(0))
            {
                pendingDrag = false;
                draggingDeploy = -1;
                draggingFromBench = false;
            }
            return;
        }

        // 标准拖拽：按下开始，松开落子
        if (!Input.GetMouseButtonUp(0)) return;

        Vector2 releaseGrid = GetGridAtMouse();
        if (releaseGrid.x >= 0 && releaseGrid.x < deployCols && releaseGrid.y >= 0 && releaseGrid.y < deployRows)
        {
            int tx = (int)releaseGrid.x;
            int ty = (int)releaseGrid.y;
            int targetDeployIdx = FindDeployAt(tx, ty);

            if (draggingFromBench)
            {
                if (draggingDeploy >= 0 && draggingDeploy < benchUnits.Count)
                {
                    if (targetDeployIdx >= 0)
                    {
                        // 备战席 -> 棋盘：支持直接替换
                        var benchUnit = benchUnits[draggingDeploy];
                        var boardUnit = deploySlots[targetDeployIdx];
                        benchUnit.x = tx;
                        benchUnit.y = ty;
                        boardUnit.x = -1;
                        boardUnit.y = -1;
                        deploySlots[targetDeployIdx] = benchUnit;
                        benchUnits[draggingDeploy] = boardUnit;
                        AutoMergeAll();
                        RedrawPrepareBoard();
                    }
                    else if (deploySlots.Count >= GetBoardCap()) battleLog = $"上阵已满（上限{GetBoardCap()}）";
                    else
                    {
                        var moved = benchUnits[draggingDeploy];
                        moved.x = tx;
                        moved.y = ty;
                        deploySlots.Add(moved);
                        benchUnits.RemoveAt(draggingDeploy);
                        AutoMergeAll();
                        RedrawPrepareBoard();
                    }
                }
            }
            else if (draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                if (targetDeployIdx >= 0 && targetDeployIdx != draggingDeploy)
                {
                    int ox = deploySlots[draggingDeploy].x;
                    int oy = deploySlots[draggingDeploy].y;
                    deploySlots[targetDeployIdx].x = ox;
                    deploySlots[targetDeployIdx].y = oy;
                }
                deploySlots[draggingDeploy].x = tx;
                deploySlots[draggingDeploy].y = ty;
                RedrawPrepareBoard();
            }
        }
        else if (releaseGrid.x == -1)
        {
            if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                int benchIdx = (int)releaseGrid.y;
                if (benchIdx >= benchUnits.Count)
                {
                    var u = deploySlots[draggingDeploy];
                    u.x = -1;
                    u.y = -1;
                    benchUnits.Add(u);
                    deploySlots.RemoveAt(draggingDeploy);
                    AutoMergeAll();
                    RedrawPrepareBoard();
                }
                else battleLog = "该备战席格子已有棋子";
            }
        }
        else
        {
            // 体验对齐金铲铲：拖到场外可直接出售
            if (draggingFromBench && draggingDeploy >= 0 && draggingDeploy < benchUnits.Count)
            {
                SellUnit(benchUnits[draggingDeploy]);
            }
            else if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                var u = deploySlots[draggingDeploy];
                if (SellUnit(u)) RedrawPrepareBoard();
            }
        }

        isDragging = false;
        pendingDrag = false;
        draggingDeploy = -1;
        draggingFromBench = false;
    }

    #endregion
}
