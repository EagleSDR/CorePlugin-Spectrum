import INativeBuffer from "../native/INativeBuffer";
import NativeGradient from "../native/NativeGradient";
import NativeHandle from "../native/NativeHandle";
import ISpectrumSettings from "../settings/ISpectrumSettings";
import BaseView from "./BaseView";

export default class SpectrumView extends BaseView {

    constructor(handle: NativeHandle, canvas: CanvasRenderingContext2D, settings: ISpectrumSettings) {
        super(handle, canvas);
        this.settings = settings;

        //Create background and foreground gradients
        this.foreground = new NativeGradient(this.handle, this.settings.foreground_colors);
        this.background = new NativeGradient(this.handle, this.settings.background_colors);
    }

    private settings: ISpectrumSettings;

    private foreground: NativeGradient;
    private background: NativeGradient;

    Resize(width: number, height: number) {
        //Resize foreground and background
        this.foreground.Resize(height);
        this.background.Resize(height);

        //Call upper
        super.Resize(width, height);
    }

    Render(source: INativeBuffer) {
        //Paint
        this.handle.PaintSpectrum(
            this.image.GetWidth(),
            this.image.GetHeight(),
            this.image.GetBuffer(),
            source,
            this.foreground.GetBuffer(),
            this.background.GetBuffer()
        );

        //Continue
        super.Render(source);
    }

}