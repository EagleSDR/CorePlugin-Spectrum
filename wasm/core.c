typedef unsigned char uint8_t;
typedef unsigned short uint16_t;
typedef unsigned int uint32_t;

typedef struct {

    uint8_t r;
    uint8_t g;
    uint8_t b;
    uint8_t a;

} eagle_color_t;

#define MIX_COLOR_CHANNEL(a, b, aPercent, bPercent) (uint8_t)((a * aPercent) + (b * bPercent))
#define SET_COLOR_BRIGHTNESS(color, brightness) color->r = brightness; color->g = brightness; color->b = brightness;

/// <summary>
/// Mixes two colors.
/// </summary>
/// <param name="result">Output color.</param>
/// <param name="a">Color A.</param>
/// <param name="b">Color B.</param>
/// <param name="aPercent">[0-1] percent of A.</param>
void mix_colors(eagle_color_t* result, const eagle_color_t* a, const eagle_color_t* b, float aPercent) {
    //Compute B percent
    float bPercent = 1 - aPercent;

    //Mix
    result->r = MIX_COLOR_CHANNEL(a->r, b->r, aPercent, bPercent);
    result->g = MIX_COLOR_CHANNEL(a->g, b->g, aPercent, bPercent);
    result->b = MIX_COLOR_CHANNEL(a->b, b->b, aPercent, bPercent);
    result->a = MIX_COLOR_CHANNEL(a->a, b->a, aPercent, bPercent);
}

/// <summary>
/// Interpolates the input colors into the output colors, producing a gradient.
/// </summary>
/// <param name="dst">The output buffer.</param>
/// <param name="dstLen">The length of the output buffer.</param>
/// <param name="src">The input buffer.</param>
/// <param name="srcLen">The length of the input buffer.</param>
void compute_gradient(eagle_color_t* dst, uint32_t dstLen, eagle_color_t* src, uint32_t srcLen) {
    for (uint32_t i = 0; i < dstLen; i++) {
        //Compute which input color to pull from
        float interp = ((float)i / dstLen) * (srcLen - 1);
        uint32_t index = (uint32_t)interp;

        //Mix
        mix_colors(
            &dst[i],
            &src[index],
            &src[index + 1],
            interp - index
        );
    }
}

/// <summary>
/// Paints a spectrum frame to the buffer.
/// </summary>
/// <param name="width">The image width. Also the size of the input data.</param>
/// <param name="height">The image height.</param>
/// <param name="image">The image to be written to.</param>
/// <param name="data">The raw data to be read as an input. Same size as width.</param>
/// <param name="gradientForeground">The foreground gradient. Same size as height.</param>
/// <param name="gradientBackground">The background gradient. Same size as height.</param>
void paint_spectrum(uint32_t width, uint32_t height, eagle_color_t* image, uint16_t* data, eagle_color_t* gradientForeground, eagle_color_t* gradientBackground) {
    //Loop top to bottom
    eagle_color_t* cursor;
    uint32_t value_previous;
    uint32_t value;
    uint32_t x;
    uint32_t y;
    uint32_t direction;
    for (x = 0; x < width; x++) {
        //Set cursor
        cursor = &image[x];

        //Convert the current value from [0-65535] to [0-height)
        value = (data[x] * height) / 65536;

        //If this is the first iteration, immediately set previous
        if (x == 0)
            value_previous = value;

        //Fill background
        for (y = 0; y < value; y++) {
            (*cursor) = gradientBackground[y];
            cursor += width;
        }

        //Fill foreground
        for (y = value; y < height; y++) {
            (*cursor) = gradientForeground[y];
            cursor += width;
        }

        //Interpolate from old point to our point
        cursor = &image[x + (value * width)];
        y = value;
        direction = value_previous > y ? 1 : -1;
        while (y != value_previous) {
            SET_COLOR_BRIGHTNESS(cursor, 255);
            y += direction;
            cursor += width * direction;
        }

        //Shift
        value_previous = value;

        //Set point in case it wasn't set above
        SET_COLOR_BRIGHTNESS(cursor, 255);
    }
}