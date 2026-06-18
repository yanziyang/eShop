/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.cshtml',
    './wwwroot/js/**/*.js',
  ],
  corePlugins: {
    // Disable Tailwind's base reset; the app already uses normalize.css
    preflight: false,
  },
  theme: {
    extend: {},
  },
  plugins: [],
}
