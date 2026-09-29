# Bump events

`/bump` is a typed command. The server says which polarity and which bump type; the game decides how far each type moves the climber (the `bumps` section of `GameSettings`).

- Positive: gloves hit from below and lift the climber up. Card on the left edge (blue banner).
- Negative: gloves hit from above and knock the climber down. Card on the right edge (orange banner). Never costs a hit point.

## Contract

Endpoint: `GET` or `POST http://localhost:56789/bump`. The request target may carry a query string. All fields are optional.

| Field | Values | Missing means |
| --- | --- | --- |
| `polarity` | `positive` or `negative` (case-insensitive) | catalog `defaultPolarity` (Negative) |
| `type` | a catalog type id, e.g. `boxing` (case-insensitive) | catalog `defaultTypeId` (`boxing`) |
| `tag` | sender display tag; trimmed, control characters stripped, capped at 24 characters | catalog `fallbackTag` (`Guest`) |

An empty value (`polarity=`, `"tag":""`) counts as absent: it never clears a value given elsewhere. Invisible characters (control, zero-width/format, line and paragraph separators) are stripped from every value, and a leading UTF-8 BOM before a JSON body is ignored.

Sources: the query string (URL-decoded, `+` = space), then the POST body, which overrides the query. A body starting with `{` is a flat JSON object of string values (unknown keys ignored; nested values, non-string values for the three known keys or malformed JSON give `bad_body`); anything else is `application/x-www-form-urlencoded`.

Responses:

- `200 {"requestId":"...","accepted":true,"polarity":"negative","type":"boxing"}` (resolved values, lower-case)
- `400 {"error":"invalid_polarity"}`, `{"error":"unknown_type"}`, `{"error":"bad_body"}` (refused before anything is queued)
- Unchanged: `400 bad_request`, `404`, `405`, `409 not_playing`, `413`, `429`, `503 timeout`
- `503 {"error":"busy"}`: more than `BumpListener.MaxConcurrentClients` (20) connections open at once; answered immediately without reading the request

Timing: an accepted bump does not move the climber at once. The move starts when the first glove lands, `GameSession.bumpImpactDelaySeconds` (0.275 s) after acceptance; `BumpBurstView` times its first glove's mid-flight to that same value. The climber then eases to the new height over `PlayerMotor.bumpMoveSeconds` (0.6 s, longer than a hazard's 0.35 s), and the camera follows at a quarter of its usual rate while the move plays (`CameraFollow.bumpFollowFactor`) so the movement is visible on screen. Input stays locked for `bumpMoveSeconds + regripSeconds`. A bump still waiting for its impact freezes while the game is paused and is dropped when a level starts or restarts. An upward bump plays a boosted pose (arms overhead); a downward one and hazard hits play the flail.

When the app goes to the background the session pauses, so `/bump` answers `409 not_playing` instead of timing out; returning to the app resumes it (there is no pause UI).

Stacking: a bump that arrives while an earlier bump is still easing adds its distance to that bump's target (two rapid +2 body-height bumps end 4 body heights up), clamped to the tower. Bumps always apply, even during a hit's invulnerability, and never cost a hit point. Long tags are cut to one line with an ellipsis; card text is plain, so markup such as `<size=900>` shows literally.

## Examples

```sh
# Bare request: default negative boxing bump (the full glove burst the brief asks for)
curl -X POST http://localhost:56789/bump

# Query string
curl "http://localhost:56789/bump?polarity=positive&type=boxing&tag=TikTikBox"

# JSON body
curl -X POST http://localhost:56789/bump -H "Content-Type: application/json" \
  -d '{"polarity":"negative","type":"boxing","tag":"TikTikBox"}'

# Form body
curl -X POST http://localhost:56789/bump -d "polarity=positive&type=boxing&tag=TikTikBox"
```

On a phone or emulator, forward the port first so a PC-side curl reaches the device: `adb forward tcp:56789 tcp:56789`. The port is `webhook.port` in `GameSettings`.

## Adding a bump type

Data only, no code. Open `Assets/Game/Settings/GameSettings.asset` and add an entry to `bumps.types`:

- `id`: what the server sends as `type`
- `displayName`: card detail line (`<name>*1`)
- `icon` and `iconTint`: card badge and burst projectiles
- `liftBodyHeights` / `dropBodyHeights`: climber body heights (feet to head, `GameSession.hazardBodyHeight` world units) moved up by a positive bump / down by a negative one. The boxing type is 2 / 1.75 (`BumpCatalog.DefaultLiftBodyHeights` / `DefaultDropBodyHeights`), raised from 1 / 1 so a social bump reads as the strongest feedback moment. These are not scaled by the climb pace

The scene builder only fills the settings asset when it creates it, so rebuilding the scene never overwrites tuned values.
