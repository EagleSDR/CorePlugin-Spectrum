mkdir -p build
cd build
clang --target=wasm32 -emit-llvm -c ../core.c -O3
llc -march=wasm32 -filetype=obj -O=3 core.bc
wasm-ld-10 --no-entry --export-all -o ../../assets/spectrum.wasm core.o
cd ..
