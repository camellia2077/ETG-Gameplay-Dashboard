# Dashboard UI Theme Catalog

The General page exposes the selected theme through a theme button. Built-in themes are ordered as `theme1` (`Default`/`默认`), `theme4` (`Snowfield`/`雪原`), `theme2` (`Mars Relic`/`火星圣物`), `theme3` (`Cyberpunk`/`赛博朋克`), and `theme5` (`Hazard`/`警戒`).

## Core Colors

The first three tokens define the general palette, the next two define category-button unselected and selected backgrounds, and the final four define ordinary and category-button borders for unselected and selected states. The six `Pickup Info` colors are dedicated high-saturation colors for nearby pickup information section labels. Default prioritizes luminous, highly saturated labels that stand out from its medium-gray `Primary`; the lighter Snowfield, Mars Relic, and Hazard backgrounds target approximately `4.5:1` label contrast or better. Cyberpunk keeps its luminous colors against its dark `Primary` background.

| Theme | Primary | Secondary | Outline | Category Unselected | Category Selected | Button Unselected Border | Button Selected Border | Category Unselected Border | Category Selected Border | Quality | Type | Summary | Effects | Synergies | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Default | `#ADA9A4` | `#8C1C13` | `#F2AA3F` | `#8C1C13` | `#972CCE` | `#949390` | `#FBF0D2` | `#F2AA3F` | `#FFFFFF` | `#DE5F75` | `#45B6DB` | `#63DD70` | `#DE9642` | `#45D7C1` | `#D144DB` |
| Snowfield | `#E5E9F0` | `#4C566A` | `#2E3440` | `#D8DEE9` | `#4C566A` | `#B8C1D1` | `#4C566A` | `#81A1C1` | `#FFFFFF` | `#D01141` | `#0D6DA5` | `#0D7722` | `#AE4B09` | `#0A766B` | `#9E18EC` |
| Mars Relic | `#E8E2D0` | `#9E1B1B` | `#C78C25` | `#D8C9B8` | `#9E1B1B` | `#A68F78` | `#F2E5C7` | `#C78C25` | `#FFF3D6` | `#C51642` | `#0D6AA0` | `#0D7321` | `#A54A0D` | `#097167` | `#9819E1` |
| Cyberpunk | `#2D005F` | `#00FF00` | `#BD00FF` | `#66FF66` | `#00FF00` | `#6F3FAF` | `#00FF00` | `#BD00FF` | `#2D005F` | `#FFFF00` | `#00D9FF` | `#FF4DFF` | `#7CFF00` | `#00FFFF` | `#FF8C00` |
| Hazard | `#E0E0E0` | `#D64011` | `#3B444B` | `#F2B39D` | `#D64011` | `#9BA3A8` | `#3B444B` | `#D64011` | `#FFFFFF` | `#C11540` | `#11699C` | `#0D7321` | `#A14B12` | `#0C6E64` | `#9612E2` |

## Adding Future Themes

Add new theme IDs to `DashboardThemeCatalog` and provide the nine core tokens plus six `Pickup Info` colors to `DashboardTheme`. Keep derived-color rules centralized in `DashboardTheme`. Category and status text colors use runtime background-luminance contrast to choose pure black or pure white.
