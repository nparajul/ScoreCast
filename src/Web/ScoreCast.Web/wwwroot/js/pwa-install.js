let deferredPrompt = null;
let dotnetRef = null;

window.pwaInstall = {
    init(ref) {
        dotnetRef = ref;
        window.addEventListener('beforeinstallprompt', e => {
            e.preventDefault();
            deferredPrompt = e;
            dotnetRef?.invokeMethodAsync('OnInstallAvailable');
        });
        window.addEventListener('appinstalled', () => {
            deferredPrompt = null;
            dotnetRef?.invokeMethodAsync('OnInstalled');
        });
    },
    async prompt() {
        if (!deferredPrompt) return false;
        deferredPrompt.prompt();
        const { outcome } = await deferredPrompt.userChoice;
        deferredPrompt = null;
        return outcome === 'accepted';
    },
    isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    },
    isIos() {
        return /iphone|ipad|ipod/i.test(navigator.userAgent) && !window.MSStream;
    }
};
