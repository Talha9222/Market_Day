# Audio clips

Drop clips straight into this folder. They are loaded by filename at runtime — no
Inspector wiring, no prefab, no code change. Any clip that is absent is simply silent.

| Filename | When it plays | Suggested |
|---|---|---|
| `theme.*` | Loops on the menu and during a scenario | Warm strings / light jazz, loopable |
| `click.*` | Every button press | Short brass/wood tap |
| `investigate.*` | Spending a research token to reveal a clue | Paper flip, folio page turn |
| `buy.*` | Committing capital | Ink stamp thud |
| `sell.*` | Closing a position | Softer stamp, lower pitch |
| `event.*` | A market event fires | Brass bell strike / card pin |
| `win.*` | Scenario objective met | Short rising brass flourish |
| `lose.*` | Objective missed | Low, brief, not punishing |

Any format Unity imports works (`.wav`, `.mp3`, `.ogg`). `.wav` is best for the short
effects, and a compressed format is better for `theme`.

Names are defined as constants in `Scripts/Core/MDAudio.cs` — change them there if you
want different filenames.

**Import settings worth setting once clips are in:** short effects → Load Type
*Decompress On Load*, Force To Mono; `theme` → *Streaming*.
