// could be running in a Firefox extension background page or a Chrome extension ServiceWorker
// Runtime
browser.runtime.onInstalled.addListener(async (e) => {
    console.log('runtime.onInstalled', e);
});
browser.runtime.onStartup.addListener((e) => {
    console.log(`runtime.onStartup`, e);
});
browser.runtime.onSuspend.addListener((e) => {
    console.log(`runtime.onSuspend`, e);
});
