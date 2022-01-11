import EagleLoggable from "../../sdk/EagleLoggable";
import IEaglePluginContext from "../../sdk/plugin/IEaglePluginContext";
import INativeBuffer from "./INativeBuffer";
declare function fetch(url: string): any;
declare namespace WebAssembly {
    function compile(source: any): Promise<any>;
    class Instance {
        constructor(module: any);
    }
}

const VM_PAGE_SIZE = 65536;

export default class NativeHandle extends EagleLoggable {

    constructor(wasmModule: any) {
        super("SpectrumNativeHandle");

        //Create instance
        this.instance = new WebAssembly.Instance(wasmModule);

        //Create memory manager
        this.memory = new NativeMemoryManager(this.instance.exports.memory, this.instance.exports.__heap_base.value);
    }

    private instance: any;
    private memory: NativeMemoryManager;

    PaintSpectrum(width: number, height: number, image: INativeBuffer, data: INativeBuffer, gradientForeground: INativeBuffer, gradientBackground: INativeBuffer) {
        this.instance.exports.paint_spectrum(width, height, image.GetPointer(), data.GetPointer(), gradientForeground.GetPointer(), gradientBackground.GetPointer());
    }

    ComputeGradient(dst: INativeBuffer, dstCount: number, src: INativeBuffer, srcCount: number): void {
        this.instance.exports.compute_gradient(dst.GetPointer(), dstCount, src.GetPointer(), srcCount);
    }

    Malloc(size: number): INativeBuffer {
        return this.memory.Allocate(size);
    }

    ClearMemory(): void {
        return this.memory.ClearMemory();
    }

    /* LOADING */

    static async CreateAsync(plugin: IEaglePluginContext): Promise<NativeHandle> {
        //Load and compile if we haven't started already
        if (this.loader == null)
            this.loader = this.LoadInternalAsync(plugin);

        //Load
        var module = await this.loader;

        return new NativeHandle(module);
    }

    private static loader: Promise<any>;

    private static async LoadInternalAsync(plugin: IEaglePluginContext): Promise<any> {
        //Download the asset
        var asset = await plugin.GetAsset("spectrum.wasm").DownloadAsBinary();

        //Initiate
        return await WebAssembly.compile(asset);
    }

}

class NativeMemoryManager {

    constructor(memory: any, memoryStart: number) {
        this.memory = memory;
        this.memoryStart = memoryStart;
        this.position = memoryStart;
    }

    private memory: any;
    private memoryStart: number;
    private position: number;
    private blocks: NativeMemoryBlock[] = [];

    GetUse(): number {
        return this.position - this.memoryStart;
    }

    //Allocates a block of size bytes and just returns the pointer. Internal use only.
    Allocate(size: number): NativeMemoryBlock {
        //Check if we currently have enough free space
        var nextHeap = this.position + size;
        if (nextHeap >= this.memory.buffer.byteLength) {
            //Determine the number of pages we need to request in order to fit
            var pages = Math.ceil((nextHeap - this.memory.buffer.byteLength) / VM_PAGE_SIZE);

            //Request these pages
            this.memory.grow(pages);

            //Growing memory pages invalidates other views...so notify all
            for (var i = 0; i < this.blocks.length; i++)
                this.blocks[i].NotifyUpdated();
        }

        //Wrap
        var view = new NativeMemoryBlock(this.memory, this.position, size);

        //Store
        this.blocks.push(view);

        //Advance heap
        this.position = nextHeap;

        return view;
    }

    ClearMemory() {
        //Clear blocks
        for (var i = 0; i < this.blocks.length; i++)
            this.blocks[i].free = true;
        this.blocks = [];

        //Reset
        this.position = this.memoryStart;
    }

}

class NativeMemoryBlock implements INativeBuffer {

    constructor(memory: any, offset: number, size: number) {
        this.memory = memory;
        this.offset = offset;
        this.size = size;
        this.free = false;
        this.NotifyUpdated();
    }

    private memory: any;
    offset: number;
    size: number;
    free: boolean;

    NotifyUpdated(): void {
        
    }

    GetPointer(): number {
        return this.offset;
    }

    GetSize(): number {
        return this.size;
    }

    AsUInt8Array(): Uint8Array {
        if (this.free)
            throw Error("Memory block was freed.");
        return new Uint8Array(this.memory.buffer, this.offset, this.size);
    }

    AsUInt8ClampedArray(): Uint8ClampedArray {
        if (this.free)
            throw Error("Memory block was freed.");
        return new Uint8ClampedArray(this.memory.buffer, this.offset, this.size);
    }

    AsUInt16Array(): Uint16Array {
        if (this.free)
            throw Error("Memory block was freed.");
        return new Uint16Array(this.memory.buffer, this.offset, this.size / 2);
    }

    Free() {
        //Set flag. It'll be cleaned up later
        this.free = true;
    }

}