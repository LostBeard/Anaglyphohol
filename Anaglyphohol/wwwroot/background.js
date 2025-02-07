
importScripts("/app/_content/SpawnDev.BlazorJS.BrowserExtension/browser-polyfill.min.js");
importScripts("/app/background.common.js");
// requires a patched version of Blazor _framework due to tight CSP rules in extension service workers
// <WebWorkerPatchFramework>true</WebWorkerPatchFramework>
// requires registering a serviceworker in the Blazor WASM Program.cs init code.
importScripts('/app/spawndev.blazorjs.webworkers.js');

