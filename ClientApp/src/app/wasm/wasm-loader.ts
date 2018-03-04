// credit to https://github.com/1337programming/wasm-angular-example/blob/master/src/app/shared/wasm/wasm-loader.ts


import { WasmImports } from './wasm-imports.interface';
import { WasmCode } from './wasm-code.interface';
import { Injectable, NgZone } from '@angular/core';

// 0. Include the WASM file in the build.
require('!!file-loader?name=main.wasm!../../../../build/main.wasm');

@Injectable()
export class WasmLoader {

    async loadWasm(wasmImports: WasmImports): Promise<WasmCode> {
        // 1. Load the wasm file.
        const wasmFile = await fetch('program.wasm').then(response => response.arrayBuffer()).then(bytes => WebAssembly.instantiate(bytes, {env: imports() }));

        // 2. Get the Array Buffer
        const buffer = await wasmFile.arrayBuffer();

        // 3. Compile the buffer.
        const mod = await WebAssembly.compile(buffer);
        const imports = this.buildImports(wasmImports);

        // 3. Initiate, passing in the bytes source and the input params.
        const wasm = new WebAssembly.Instance(mod, imports);

        // 4. Return the exports, as the defined WasmCode.
        return wasm.exports as WasmCode;
}
