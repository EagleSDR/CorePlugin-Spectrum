import INativeBuffer from "./INativeBuffer";
import NativeHandle from "./NativeHandle";

export default class NativeGradient {

    constructor(handle: NativeHandle, colors: number[][]) {
        this.handle = handle;
        this.colors = colors;
    }

    private handle: NativeHandle;
    private colors: number[][];
    private buffer: INativeBuffer;

    Resize(size: number): void {
        //Free old buffer
        if (this.buffer != null)
            this.buffer.Free();

        //Allocate output buffer
        this.buffer = this.handle.Malloc(size * 4);

        //Compute the gradient
        NativeGradient.ComputeGradient(this.buffer.AsUInt8Array(), size, this.colors, this.colors.length);
    }

    GetBuffer(): INativeBuffer {
        return this.buffer;
    }

    private static ComputeGradient(dst: Uint8Array, dstLen: number, src: number[][], srcLen: number) {
        for (var i = 0; i < dstLen; i++) {
            //Compute which input color to pull from
            var interp = (i / dstLen) * (srcLen - 1);
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

}