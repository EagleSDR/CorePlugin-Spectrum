import INativeBuffer from "./INativeBuffer";
import NativeHandle from "./NativeHandle";

export default class NativeCanvas {

    constructor(handle: NativeHandle) {
        this.handle = handle;
    }

    private handle: NativeHandle;
    private width: number;
    private height: number;
    private buffer: INativeBuffer;

    private data: ImageData; //DO NOT GET DIRECTLY

    Resize(width: number, height: number) {
        //Free old buffer
        if (this.buffer != null)
            this.buffer.Free();

        //Set
        this.width = width;
        this.height = height;

        //Allocate
        this.buffer = this.handle.Malloc(4 * width * height);

        //Invalidate image data
        this.data = null;
    }

    private EnsureReady() {
        if (this.buffer == null)
            throw Error("Image has not been created yet. Be sure to call resize().");
    }

    private GetImageData(): ImageData {
        //Create if it doesn't exist
        if (this.data == null) {
            //Log
            console.log("making image...");
            console.log(this.buffer.AsUInt8Array());

            //Sanity check
            this.EnsureReady();

            //Create
            this.data = new ImageData(this.buffer.AsUInt8ClampedArray(), this.width, this.height);
        }

        return this.data;
    }

    GetWidth() {
        this.EnsureReady();
        return this.width;
    }

    GetHeight() {
        this.EnsureReady();
        return this.height;
    }

    GetBuffer(): INativeBuffer {
        return this.buffer;
    }

    Render(canvas: CanvasRenderingContext2D, offsetX: number, offsetY: number) {
        //Write
        canvas.putImageData(this.GetImageData(), offsetX, offsetY);
    }

}