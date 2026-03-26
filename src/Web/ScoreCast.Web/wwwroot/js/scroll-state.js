window.scrollState = {
    positions: {},
    save(key) {
        this.positions[key] = window.scrollY || document.documentElement.scrollTop;
    },
    restore(key) {
        const y = this.positions[key];
        if (y > 0) {
            requestAnimationFrame(() => window.scrollTo(0, y));
        }
    },
    init() {
        let currentPath = location.pathname;
        window.addEventListener('scroll', () => {
            this.positions[location.pathname] = window.scrollY || document.documentElement.scrollTop;
        }, { passive: true });
    }
};
scrollState.init();
