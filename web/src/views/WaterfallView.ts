import INativeBuffer from "../native/INativeBuffer";
import IWaterfallSettings from "../settings/IWaterfallSettings";
import { ComputeGradient } from "../util/GradientGenerator";

export default class WaterfallView {

    constructor(canvas: CanvasRenderingContext2D, settings: IWaterfallSettings) {
        this.canvas = canvas;
        this.settings = settings;

        //Allocate lookup table and fill
        this.lookupTable = new Uint8Array(256 * 4);
        this.ComputeColors();
    }

    private canvas: CanvasRenderingContext2D;
    private settings: IWaterfallSettings;

    private lookupTable: Uint8Array;
    private image: ImageData;
    private width: number;
    private height: number;

    private line: number = 0;

    Resize(width: number, height: number) {
        //Create new image data
        this.width = width;
        this.height = height;
        this.image = new ImageData(width, height);
    }

    Render(source: INativeBuffer) {
        //Render the top-most part of the image
        this.canvas.putImageData(this.image, 0, this.line, )
    }

    private ComputeColors() {
        ComputeGradient(this.settings.colors, this.lookupTable, 256);
    }

}