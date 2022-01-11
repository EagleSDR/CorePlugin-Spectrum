export default interface INativeBuffer {

    GetPointer(): number;
    GetSize(): number;

    AsUInt8Array(): Uint8Array;
    AsUInt8ClampedArray(): Uint8ClampedArray;
    AsUInt16Array(): Uint16Array;

    Free(): void;

}