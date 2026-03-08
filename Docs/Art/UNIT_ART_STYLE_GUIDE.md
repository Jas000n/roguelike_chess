# 棋子统一美术规范（DrawThings / qwen）

适用版本：DragonChessLegends v0.2+  
目的：保证所有棋子是“同一套画风”，避免混用导致风格割裂。

## 1. 生成入口
- 脚本：`Docs/ContentPack_v0.2/generate_units_unified_qwen.py`
- 默认接口：`http://127.0.0.1:7860`
- 默认模型：`qwen_image_2512_q6p.ckpt`

运行命令：

```bash
DRAWTHINGS_URL=http://127.0.0.1:7860 \
DRAWTHINGS_MODEL=qwen_image_2512_q6p.ckpt \
python3 Docs/ContentPack_v0.2/generate_units_unified_qwen.py
```

输出目录：
- `Assets/Resources/Art/Units/unit_<unit_key>.png`

## 2. 统一风格锚点（必须保留）
- 题材：新国风 + 象棋机甲幻想
- 构图：单体居中、可读轮廓、禁止多主体
- 画面：深海军蓝背景 + 受控高光
- 资产性质：纯棋子 portrait，不含 UI 边框、文案、logo

## 3. Prompt 结构（推荐模板）
- 主体描述：`single unit portrait of <unit identity>`
- 识别要求：`strategy autobattler unit identity readable at thumbnail size`
- 一致性要求：`consistent brushwork and lighting with the full set`
- 全局风格：由脚本中的 `STYLE` 统一注入

## 4. Negative Prompt（不要删）
- 必须过滤：文字、水印、整屏UI、拼贴、多主体、低清、畸形、写实照片感
- 原因：这些问题会直接导致按钮/UI误识别和棋子风格不统一

## 5. 出图参数建议
- 1024x1024
- steps: 10~14
- cfg: 6.0~7.0
- sampler: `DPM++ 2M Karras`（一致性更稳）

## 6. 验收清单
- 同一批次 22 张单位图是否全部重画
- 缩略图（小尺寸）下是否一眼区分“车/马/炮/士/兵”
- 是否出现文字、边框、整屏界面元素
- 明暗/对比是否一致，避免某几张“发灰”或“过曝”

## 7. 迭代规则
- 小修：只重画问题 unit
- 大修：整套 22 张同批次重画，禁止混批混风格
- 每次重画后记录：模型名、参数、日期、问题与取舍
