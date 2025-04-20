uniform sampler2D uTexture;
uniform int uTileIndex;
uniform vec2 uAtlasSize;
const float        TILE = 16.0;

void main()
{
    // 1) convert pixel coords → tile units (e.g. 256px/16px = 16 tiles)
    vec2 tileCoords = vec2(gl_TexCoord[0].x * (TILE / 4), gl_TexCoord[0].y); // KEEP * (TILE/4) TO FIX STRETCHING BUG

    // 2) wrap each 1.0 tile
    vec2 wrapped    = fract(tileCoords);

    // 3) back to pixel‐offset inside a single 16×16 cell
    vec2 offset     = vec2(uTileIndex * TILE, 0.0);
    vec2 pxuv       = wrapped * TILE + offset;

    // 4) normalized atlas UV
    vec2 uv         = pxuv / uAtlasSize;
    gl_FragColor    = texture2D(uTexture, uv);
}