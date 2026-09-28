# Bump events

`/bump` is a typed command. The server says which polarity and which bump type; the game decides how far each type moves the climber (`BumpCatalog`).

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

Stacking: a bump that arrives while an earlier bump is still easing adds its distance to that bump's target (two rapid +1.5 bumps end 3.0 up, before pace scaling), clamped to the tower. Bumps always apply, even during a hit's invulnerability, and never cost a hit point. Long tags are cut to one line with an ellipsis; card text is plain, so markup such as `<size=900>` shows literally.

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

On a phone or emulator, forward the port first so a PC-side curl reaches the device: `adb forward tcp:56789 tcp:56789`.

## Adding a bump type

Data only, no code. Open `Assets/Game/Levels/BumpCatalog.asset` and add an entry to `types`:

- `id`: what the server sends as `type`
- `displayName`: card detail line (`<name>*1`)
- `icon` and `iconTint`: card badge and burst projectiles
- `liftDistance` / `dropDistance`: authored level units moved up by a positive bump / down by a negative one (scaled by the climb pace like every level distance)

The scene builder only fills the catalog when it creates it, so rebuilding the scene never overwrites tuned values.
