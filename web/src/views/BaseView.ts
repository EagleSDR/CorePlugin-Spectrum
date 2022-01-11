import INativeBuffer from "../native/INativeBuffer";
import NativeCanvas from "../native/NativeCanvas";
import NativeHandle from "../native/NativeHandle";

export default class BaseView {

    constructor(handle: NativeHandle, canvas: CanvasRenderingContext2D) {
        this.handle = handle;
        this.canvas = canvas;
        this.image = new NativeCanvas(this.handle);
    }

    protected handle: NativeHandle;
    protected image: NativeCanvas;

    private canvas: CanvasRenderingContext2D;

    Resize(width: number, height: number) {
        //Resize image
        this.image.Resize(width, height);
    }

    Render(source: INativeBuffer) {
        //Draw frame
        this.image.Render(this.canvas, 0, 0);
    }

}