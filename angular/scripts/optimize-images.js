#!/usr/bin/env node

/**
 * Image Optimization Script for Angular Application
 * 
 * Converts images to multiple formats (WebP, AVIF) and creates responsive variants
 * Usage: node optimize-images.js
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import sharp from 'sharp';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const ASSET_DIR = path.join(__dirname, '../src/assets/common/images');
const OUTPUT_DIR = ASSET_DIR;

// Image sizes to generate (in pixels)
const RESPONSIVE_SIZES = [320, 640, 960, 1280, 1920];

// Supported output formats
const FORMATS = {
  jpg: { quality: 80 },
  webp: { quality: 80 },
  avif: { quality: 70 }
};

async function optimizeImages() {
  console.log('🖼️  Starting image optimization...\n');

  if (!fs.existsSync(ASSET_DIR)) {
    console.log(`⚠️  Asset directory not found: ${ASSET_DIR}`);
    return;
  }

  const files = fs.readdirSync(ASSET_DIR)
    .filter(file => /\.(jpg|jpeg|png|webp)$/i.test(file));

  if (files.length === 0) {
    console.log('⚠️  No images found in assets directory');
    return;
  }

  console.log(`📁 Found ${files.length} images to optimize:\n`);

  let totalOriginalSize = 0;
  let totalOptimizedSize = 0;

  for (const file of files) {
    const inputPath = path.join(ASSET_DIR, file);
    const fileSize = fs.statSync(inputPath).size;
    totalOriginalSize += fileSize;

    console.log(`Processing: ${file}`);
    const baseFileName = path.parse(file).name;

    try {
      // Process each size variant
      for (const size of RESPONSIVE_SIZES) {
        // JPEG variant
        const jpgOutput = path.join(OUTPUT_DIR, `${baseFileName}-${size}w.jpg`);
        await sharp(inputPath)
          .resize(size, size, { fit: 'cover', withoutEnlargement: true })
          .jpeg(FORMATS.jpg)
          .toFile(jpgOutput);

        // WebP variant
        const webpOutput = path.join(OUTPUT_DIR, `${baseFileName}-${size}w.webp`);
        await sharp(inputPath)
          .resize(size, size, { fit: 'cover', withoutEnlargement: true })
          .webp(FORMATS.webp)
          .toFile(webpOutput);

        // AVIF variant
        const avifOutput = path.join(OUTPUT_DIR, `${baseFileName}-${size}w.avif`);
        await sharp(inputPath)
          .resize(size, size, { fit: 'cover', withoutEnlargement: true })
          .avif(FORMATS.avif)
          .toFile(avifOutput);

        const jpgSize = fs.statSync(jpgOutput).size;
        const webpSize = fs.statSync(webpOutput).size;
        const avifSize = fs.statSync(avifOutput).size;

        totalOptimizedSize += jpgSize + webpSize + avifSize;

        console.log(`  ${size}w: JPG=${(jpgSize/1024).toFixed(1)}KB | WebP=${(webpSize/1024).toFixed(1)}KB | AVIF=${(avifSize/1024).toFixed(1)}KB`);
      }

      console.log(`  ✅ ${baseFileName}: Optimized with 3 size variants × 3 formats\n`);
    } catch (error) {
      console.error(`  ❌ Error processing ${file}:`, error.message);
    }
  }

  // Summary statistics
  console.log('📊 Optimization Complete!');
  console.log('─'.repeat(50));
  console.log(`Original total size: ${(totalOriginalSize / 1024 / 1024).toFixed(2)} MB`);
  console.log(`Optimized total size: ${(totalOptimizedSize / 1024 / 1024).toFixed(2)} MB`);
  console.log(`Compression ratio: ${((1 - totalOptimizedSize / totalOriginalSize) * 100).toFixed(1)}%`);
  console.log('─'.repeat(50));
}

// Run optimization
optimizeImages().catch(error => {
  console.error('Fatal error:', error);
  process.exit(1);
});
