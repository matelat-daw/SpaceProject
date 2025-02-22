document.addEventListener("DOMContentLoaded", function () {
    const themeToggleButton = document.getElementById("theme-toggle");
    let currentTheme = localStorage.getItem("theme") || "light";

    setTheme(currentTheme);

    themeToggleButton.addEventListener("click", function () {
        currentTheme = currentTheme === "light" ? "dark" : "light";
        setTheme(currentTheme);
        localStorage.setItem("theme", currentTheme);
    });

    function setTheme(theme) {
        const lightThemeLink = document.getElementById("light-theme");
        const darkThemeLink = document.getElementById("dark-theme");

        if (theme === "dark") {
            lightThemeLink.disabled = true;
            darkThemeLink.disabled = false;
        } else {
            lightThemeLink.disabled = false;
            darkThemeLink.disabled = true;
        }
    }
});