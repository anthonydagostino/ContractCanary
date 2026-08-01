/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // Signature warm gold — the "canary". Used for brand marks, accents, highlights.
        canary: {
          50: '#fffbea', 100: '#fff3c4', 200: '#fce588', 300: '#fadb5f',
          400: '#f7c948', 500: '#f0b429', 600: '#de911d', 700: '#cb6e17',
          800: '#b44d12', 900: '#8d2b0b',
        },
        // Deep, trustworthy navy — primary actions, dark surfaces, headings.
        ink: {
          50: '#eef1f6', 100: '#d6dce7', 200: '#b0bcd0', 300: '#8496b4',
          400: '#5a6d8f', 500: '#3c4f70', 600: '#2b3a55', 700: '#1e2a40',
          800: '#141d2e', 900: '#0b1220',
        },
        // Retained informational blue (badges/alerts/links inside the app).
        brand: {
          50: '#eff6ff', 100: '#dbeafe', 200: '#bfdbfe', 300: '#93c5fd',
          400: '#60a5fa', 500: '#3b82f6', 600: '#2563eb', 700: '#1d4ed8',
          800: '#1e40af', 900: '#1e3a8a',
        },
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      boxShadow: {
        soft: '0 1px 2px 0 rgb(11 18 32 / 0.04), 0 4px 16px -4px rgb(11 18 32 / 0.08)',
        lift: '0 8px 30px -8px rgb(11 18 32 / 0.18)',
      },
      keyframes: {
        'fade-up': {
          '0%': { opacity: '0', transform: 'translateY(8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
      },
      animation: {
        'fade-up': 'fade-up 0.5s ease-out both',
      },
    },
  },
  plugins: [],
}
