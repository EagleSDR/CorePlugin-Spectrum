export function ComputeGradient(src: number[][], dst: Uint8Array, dstLen: number) {
    for (var i = 0; i < dstLen; i++) {
        //Compute which input color to pull from
        var interp = (i / dstLen) * (src.length - 1);
        var index = Math.floor(interp);

        //Calculate mix percents
        var aPercent = interp - index;
        var bPercent = 1 - aPercent;

        //Mix each channel
        for (var c = 0; c < 3; c++)
            dst[(i * 4) + c] = (src[index][c] * aPercent) + (src[index + 1][c] * bPercent);
        dst[(i * 4) + 3] = 255;
    }
}