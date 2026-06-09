import sharp from 'sharp'
import { mkdirSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const RES = join(__dirname, 'resources')
mkdirSync(RES, { recursive: true })

// ── SVG icona MyCars (1024x1024) ─────────────────────────────────────────────
// Sfondo navy con angoli arrotondati, auto bianca centrata, punto rosso in basso
const iconSvg = `<svg width="1024" height="1024" xmlns="http://www.w3.org/2000/svg">
  <!-- Sfondo navy arrotondato -->
  <rect width="1024" height="1024" rx="220" fill="#1E3A5F"/>

  <!-- Accento rosso in basso -->
  <rect x="0" y="820" width="1024" height="204" rx="0" fill="#D62828" opacity=".18"/>
  <rect x="0" y="900" width="1024" height="124" rx="0" fill="#D62828" opacity=".22"/>

  <!-- Auto stilizzata (centrata, bianca) -->
  <g transform="translate(140, 320) scale(3.0)">
    <!-- Carrozzeria -->
    <path d="M18 52 L26 28 Q30 20 38 18 L206 18 Q218 18 224 26 L248 52 L260 52
             L260 84 Q260 88 256 88 L238 88 Q234 88 234 84 L234 78 L50 78
             L50 84 Q50 88 46 88 L28 88 Q24 88 24 84 L24 52 Z"
          fill="white"/>
    <!-- Finestrini -->
    <path d="M58 18 L48 60 L142 60 L142 18 Z"
          fill="rgba(30,58,95,0.55)" rx="4"/>
    <path d="M148 18 L148 60 L216 60 L236 52 L220 28 Z"
          fill="rgba(30,58,95,0.45)"/>
    <!-- Linea porta -->
    <line x1="144" y1="18" x2="144" y2="78"
          stroke="rgba(30,58,95,0.3)" stroke-width="3"/>
    <!-- Ruota sinistra -->
    <circle cx="74"  cy="88" r="26" fill="#152B47"/>
    <circle cx="74"  cy="88" r="16" fill="#2A4D73"/>
    <circle cx="74"  cy="88" r="7"  fill="#8A9BB8"/>
    <!-- Ruota destra -->
    <circle cx="218" cy="88" r="26" fill="#152B47"/>
    <circle cx="218" cy="88" r="16" fill="#2A4D73"/>
    <circle cx="218" cy="88" r="7"  fill="#8A9BB8"/>
    <!-- Fanale anteriore -->
    <path d="M248 52 L262 62 L262 78 L248 78 Z"
          fill="rgba(255,220,100,0.7)"/>
    <!-- Fanale posteriore -->
    <path d="M28 62 L18 66 L18 78 L30 78 Z"
          fill="rgba(214,40,40,0.7)"/>
  </g>

  <!-- Punto rosso in basso (brand accent) -->
  <circle cx="512" cy="880" r="28" fill="#D62828"/>
</svg>`

// ── SVG foreground per adaptive icon (trasparente) ────────────────────────────
const fgSvg = `<svg width="1024" height="1024" xmlns="http://www.w3.org/2000/svg">
  <g transform="translate(140, 320) scale(3.0)">
    <path d="M18 52 L26 28 Q30 20 38 18 L206 18 Q218 18 224 26 L248 52 L260 52
             L260 84 Q260 88 256 88 L238 88 Q234 88 234 84 L234 78 L50 78
             L50 84 Q50 88 46 88 L28 88 Q24 88 24 84 L24 52 Z"
          fill="white"/>
    <path d="M58 18 L48 60 L142 60 L142 18 Z"
          fill="rgba(30,58,95,0.55)" rx="4"/>
    <path d="M148 18 L148 60 L216 60 L236 52 L220 28 Z"
          fill="rgba(30,58,95,0.45)"/>
    <line x1="144" y1="18" x2="144" y2="78"
          stroke="rgba(30,58,95,0.3)" stroke-width="3"/>
    <circle cx="74"  cy="88" r="26" fill="#152B47"/>
    <circle cx="74"  cy="88" r="16" fill="#2A4D73"/>
    <circle cx="74"  cy="88" r="7"  fill="rgba(255,255,255,0.4)"/>
    <circle cx="218" cy="88" r="26" fill="#152B47"/>
    <circle cx="218" cy="88" r="16" fill="#2A4D73"/>
    <circle cx="218" cy="88" r="7"  fill="rgba(255,255,255,0.4)"/>
    <path d="M248 52 L262 62 L262 78 L248 78 Z"
          fill="rgba(255,220,100,0.7)"/>
  </g>
  <circle cx="512" cy="880" r="28" fill="#D62828"/>
</svg>`

// ── Genera i file sorgente ─────────────────────────────────────────────────────
console.log('Generazione icone MyCars...')

await sharp(Buffer.from(iconSvg))
  .resize(1024, 1024)
  .png()
  .toFile(join(RES, 'icon.png'))
console.log('✓ resources/icon.png')

await sharp(Buffer.from(fgSvg))
  .resize(1024, 1024)
  .png()
  .toFile(join(RES, 'icon-foreground.png'))
console.log('✓ resources/icon-foreground.png')

await sharp({ create: { width: 1024, height: 1024, channels: 4, background: { r: 30, g: 58, b: 95, alpha: 1 } } })
  .png()
  .toFile(join(RES, 'icon-background.png'))
console.log('✓ resources/icon-background.png')

// Splash screen: navy con logo centrato
const splashSvg = `<svg width="2732" height="2732" xmlns="http://www.w3.org/2000/svg">
  <rect width="2732" height="2732" fill="#1E3A5F"/>
  <g transform="translate(900, 1050) scale(8.8)">
    <path d="M18 52 L26 28 Q30 20 38 18 L206 18 Q218 18 224 26 L248 52 L260 52
             L260 84 Q260 88 256 88 L238 88 Q234 88 234 84 L234 78 L50 78
             L50 84 Q50 88 46 88 L28 88 Q24 88 24 84 L24 52 Z"
          fill="white" opacity=".9"/>
    <circle cx="74"  cy="88" r="26" fill="#152B47"/>
    <circle cx="74"  cy="88" r="14" fill="#2A4D73"/>
    <circle cx="218" cy="88" r="26" fill="#152B47"/>
    <circle cx="218" cy="88" r="14" fill="#2A4D73"/>
  </g>
  <text x="1366" y="1820"
        font-family="Arial, sans-serif" font-size="120" font-weight="bold"
        fill="white" text-anchor="middle" opacity=".9">MyCars</text>
</svg>`

await sharp(Buffer.from(splashSvg))
  .resize(2732, 2732)
  .png()
  .toFile(join(RES, 'splash.png'))
console.log('✓ resources/splash.png')

console.log('\nFile sorgente pronti in resources/')
