import EagleUtil from "../../sdk/EagleUtil";
import IEaglePluginContext from "../../sdk/plugin/IEaglePluginContext";
import EagleWindowImplementation from "../../sdk/ui/window/EagleWindowImplementation";
import IEagleWindowContext from "../../sdk/ui/window/IEagleWindowContext";
import INativeBuffer from "../native/INativeBuffer";
import NativeHandle from "../native/NativeHandle";
import ISpectrumSettings from "../settings/ISpectrumSettings";
import SpectrumView from "../views/SpectrumView";

export default class SpectrumWindow extends EagleWindowImplementation {

    constructor(window: IEagleWindowContext, plugin: IEaglePluginContext) {
        super(window);
        this.plugin = plugin;

        //Create window
        this.SetTitle("Spectrum");
        this.canvas = EagleUtil.CreateElement("canvas", null, this.GetMount()) as HTMLCanvasElement;

        //Create handle
        this.init = this.InitAsync();
    }

    private plugin: IEaglePluginContext;
    private canvas: HTMLCanvasElement;

    private init: Promise<void>;
    private handle: NativeHandle;
    private input: INativeBuffer;
    private view: SpectrumView;

    private active: boolean;

    static EagleGetDisplayName(): string {
        return "Spectrum Base";
    }

    static EagleGetGroup(): string {
        return "Spectrum";
    }

    static EagleGetPreview(): HTMLElement {
        return EagleUtil.CreateElement("div", null);
    }

    private async InitAsync() {
        //Get handle
        this.handle = await NativeHandle.CreateAsync(this.plugin);

        //Make view
        this.view = new SpectrumView(this.handle, this.canvas.getContext('2d'), this.GetSpectrumSettings());
    }

    OnOpened(): void {
        this.active = true;
        requestAnimationFrame(() => this.RefreshFrame());
    }

    OnResized(): void {
        //Get size
        var width = this.GetWidth();
        var height = this.GetHeight();

        //Set canvas size
        this.canvas.width = width;
        this.canvas.height = height;

        //Wait until initialization is finished
        this.init.then(() => {
            //Clear old buffers
            this.handle.ClearMemory();

            //Create input buffer (of shorts)
            this.input = this.handle.Malloc(width * 2);

            //Trigger resize
            this.view.Resize(width, height);
        });
    }

    OnClosed(): void {
        this.active = false;
    }

    private RefreshFrame() {
        //Make sure we're active
        if (!this.active)
            return;

        //Draw
        if (this.view != null) {
            this.FillWithSampleData(this.input);
            this.view.Render(this.input);
        }

        //Trigger next
        requestAnimationFrame(() => this.RefreshFrame());
    }

    private GetSpectrumSettings(): ISpectrumSettings {
        return {
            background_colors: [
                [0, 0, 20],
                [52, 83, 117]
            ],
            foreground_colors: [
                [0, 0, 80],
                [112, 180, 255]
            ]
        }
    }

    private test: number = 0;

    private FillWithSampleData(buffer: INativeBuffer) {
        //Get as UInt16
        var data = buffer.AsUInt16Array();

        //Fill
        var z = this.test++ % 60;
        for (var i = 0; i < data.length; i++) {
            data[i] = 49152;
            if (z > 30) {
                data[i] -= (z - 30) * 1200;
            }
            z = (z + 1) % 60;
        }
    }

}