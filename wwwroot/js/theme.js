window.saludVidaTheme = {
    apply: function (primaryColor) {
        if (!primaryColor) {
            return;
        }

        const darkColor = window.saludVidaTheme.darken(primaryColor, 18);
        document.documentElement.style.setProperty("--brand", primaryColor);
        document.documentElement.style.setProperty("--brand-dark", darkColor);
        document.documentElement.style.setProperty("--color-marca", primaryColor);

        const themeMeta = document.querySelector('meta[name="theme-color"]');
        if (themeMeta) {
            themeMeta.setAttribute("content", primaryColor);
        }
    },
    darken: function (hex, amount) {
        const normalized = hex.replace("#", "");
        if (normalized.length !== 6) {
            return hex;
        }

        const value = parseInt(normalized, 16);
        let r = (value >> 16) - amount;
        let g = ((value >> 8) & 0x00ff) - amount;
        let b = (value & 0x0000ff) - amount;

        r = Math.max(0, Math.min(255, r));
        g = Math.max(0, Math.min(255, g));
        b = Math.max(0, Math.min(255, b));

        return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
    }
};
