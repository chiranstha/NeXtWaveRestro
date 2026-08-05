module.exports = {
  content: [
    './src/**/*.{html,ts,jsx,tsx}',
    './src/index.html'
  ],
  css: [
    './src/styles-complete-optimized.css',
    './src/**/*.css'
  ],
  output: './src/styles-purged.css',
  rejected: false,
  variables: true,
  dynamicAttributes: [
    'aria-expanded',
    'data-active',
    'data-visible',
    'data-state'
  ],
  keyframes: true,
  fontFace: true,
  // Preserve these selectors even if not found in content
  safelist: [
    /ng-/,
    /ngx-/,
    /p-/,
    /gu-/,
    /hljs/,
    /fa-/,
    'show',
    'active',
    'disabled',
    'open',
    'closed',
    'expanded',
    'loading',
    'error',
    'success',
    'warning',
    'info',
    /^angular-/,
    /^app-/,
    /^menu/,
    /^sidebar/,
    /^modal/,
    /^toast/,
    /^popover/,
    /^tooltip/
  ],
  blocklist: [],
  defaultExtractor: (content) => {
    // Look for all class/id/attribute patterns
    const matches = content.match(/[A-Za-z:_-[\w/-]+(?:w+\(%\"?\'?[^\)\"\']*\)?)?(?:[^\s,])?/g) || [];
    // Also match arbitrary values and dark mode patterns
    return matches.concat(content.match(/\[[^\]]+\]/g) || []);
  }
};
