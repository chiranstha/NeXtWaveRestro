#!/usr/bin/env node

/**
 * Bundle Size Tracking Script
 *
 * Tracks bundle sizes across builds and generates reports
 * Helps identify regressions and measure optimization impact
 * Usage: node track-bundle-size.js
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const TRACKING_FILE = path.join(__dirname, '../bundle-size-history.json');
const DIST_DIR = path.join(__dirname, '../dist/browser');

/**
 * @typedef {Object} BundleMetrics
 * @property {string} timestamp
 * @property {string} date
 * @property {number} totalSize
 * @property {Array<{name: string, size: number, gzipSize: number}>} bundles
 * @property {Object} [changes]
 * @property {number} changes.totalChange
 * @property {number} changes.totalChangePercent
 * @property {Array<{name: string, sizeChange: number, sizeChangePercent: number}>} changes.bundleChanges
 */

/**
 * Get size of a file or directory
 */
function getSize(filePath) {
  try {
    const stats = fs.statSync(filePath);
    if (stats.isFile()) {
      return stats.size;
    } else if (stats.isDirectory()) {
      return fs.readdirSync(filePath)
        .reduce((total, file) => total + getSize(path.join(filePath, file)), 0);
    }
  } catch (error) {
    console.error(`Error reading: ${filePath}`);
  }
  return 0;
}

/**
 * Format bytes to human-readable format
 */
function formatBytes(bytes) {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
}

/**
 * Get approximate gzip size (roughly 30% of original)
 */
function estimateGzipSize(size) {
  return Math.round(size * 0.3);
}

/**
 * Analyze bundle sizes
 */
function analyzeBundles() {
  const bundles = [];

  // Find all JavaScript bundles
  const mainFiles = fs.readdirSync(DIST_DIR)
    .filter(file => file.match(/^main-[a-z0-9]+\.js$|^polyfills-[a-z0-9]+\.js$|^styles-[a-z0-9]+\.css$/i));

  // Find lazy-loaded chunks
  const chunkDir = path.join(DIST_DIR);
  const chunks = fs.readdirSync(chunkDir)
    .filter(file => file.match(/^[a-z-]+-[a-z0-9]+\.js$/) && !mainFiles.includes(file));

  // Calculate main bundle
  const mainSize = mainFiles.reduce((total, file) => total + getSize(path.join(DIST_DIR, file)), 0);
  bundles.push({
    name: 'Main Bundle',
    size: mainSize,
    gzipSize: estimateGzipSize(mainSize)
  });

  // Calculate lazy chunks
  const chunkSizes = new Map();
  chunks.forEach(chunk => {
    const moduleName = chunk.split('-')[0];
    const size = getSize(path.join(DIST_DIR, chunk));
    const currentSize = chunkSizes.get(moduleName) || 0;
    chunkSizes.set(moduleName, currentSize + size);
  });

  chunkSizes.forEach((size, name) => {
    bundles.push({
      name: `${name} (chunk)`,
      size,
      gzipSize: estimateGzipSize(size)
    });
  });

  // Calculate total size
  const totalSize = bundles.reduce((sum, b) => sum + b.size, 0);

  return {
    timestamp: new Date().toISOString(),
    date: new Date().toLocaleDateString(),
    totalSize,
    bundles: bundles.sort((a, b) => b.size - a.size)
  };
}

/**
 * Compare with previous build
 */
function compareWithPrevious(current) {
  if (!fs.existsSync(TRACKING_FILE)) {
    return current;
  }

  try {
    const history = JSON.parse(fs.readFileSync(TRACKING_FILE, 'utf8'));
    const previous = history[history.length - 1];

    if (!previous) {
      return current;
    }

    const totalChange = current.totalSize - previous.totalSize;
    const totalChangePercent = (totalChange / previous.totalSize) * 100;

    const bundleChanges = current.bundles.map(currentBundle => {
      const prevBundle = previous.bundles.find(b => b.name === currentBundle.name);
      const prevSize = prevBundle?.size || 0;
      const sizeChange = currentBundle.size - prevSize;
      const sizeChangePercent = prevSize > 0 ? (sizeChange / prevSize) * 100 : 0;

      return {
        name: currentBundle.name,
        sizeChange,
        sizeChangePercent
      };
    });

    return {
      ...current,
      changes: {
        totalChange,
        totalChangePercent,
        bundleChanges
      }
    };
  } catch (error) {
    console.warn('Could not compare with previous build');
    return current;
  }
}

/**
 * Save metrics to history
 */
function saveMetrics(metrics) {
  let history = [];

  if (fs.existsSync(TRACKING_FILE)) {
    try {
      history = JSON.parse(fs.readFileSync(TRACKING_FILE, 'utf8'));
    } catch (e) {
      // Start fresh if file is corrupted
      history = [];
    }
  }

  history.push(metrics);
  
  // Keep only last 30 builds
  if (history.length > 30) {
    history = history.slice(-30);
  }

  fs.writeFileSync(TRACKING_FILE, JSON.stringify(history, null, 2));
}

/**
 * Print metrics report
 */
function printReport(metrics) {
  console.log('\n📊 Bundle Size Analysis');
  console.log('═'.repeat(60));
  console.log(`📅 ${metrics.date} - ${new Date(metrics.timestamp).toLocaleTimeString()}\n`);

  console.log('📦 Bundle Breakdown:');
  console.log('─'.repeat(60));
  metrics.bundles.forEach(bundle => {
    const percent = ((bundle.size / metrics.totalSize) * 100).toFixed(1);
    console.log(`  ${bundle.name.padEnd(20)} ${formatBytes(bundle.size).padStart(10)} (gzip: ${formatBytes(bundle.gzipSize).padStart(10)}) [${percent}%]`);
  });

  console.log('─'.repeat(60));
  console.log(`  ${'TOTAL'.padEnd(20)} ${formatBytes(metrics.totalSize).padStart(10)}`);
  console.log('');

  if (metrics.changes) {
    const changeSymbol = metrics.changes.totalChange >= 0 ? '⚠️ ' : '✅ ';
    const changeSign = metrics.changes.totalChange >= 0 ? '+' : '';
    console.log(`${changeSymbol}Change: ${changeSign}${formatBytes(metrics.changes.totalChange)} (${changeSign}${metrics.changes.totalChangePercent.toFixed(2)}%)\n`);
  }

  console.log('═'.repeat(60) + '\n');
}

/**
 * Main execution
 */
function main() {
  if (!fs.existsSync(DIST_DIR)) {
    console.error('❌ Distribution directory not found:', DIST_DIR);
    console.log('Please run: ng build --configuration production');
    process.exit(1);
  }

  console.log('📈 Tracking bundle sizes...\n');

  let metrics = analyzeBundles();
  metrics = compareWithPrevious(metrics);
  saveMetrics(metrics);
  printReport(metrics);

  // Alert if bundle size increased significantly
  if (metrics.changes && metrics.changes.totalChangePercent > 5) {
    console.warn('⚠️  WARNING: Bundle size increased by more than 5%!');
    console.warn('   Consider reviewing the following changes:');
    metrics.changes.bundleChanges
      .filter(c => c.sizeChangePercent > 5)
      .forEach(c => {
        console.warn(`   - ${c.name}: +${formatBytes(c.sizeChange)} (+${c.sizeChangePercent.toFixed(2)}%)`);
      });
  }
}

main();
