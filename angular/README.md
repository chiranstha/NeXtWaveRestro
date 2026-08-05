## ASP.NET ZERO - Angular UI

See documentation: https://www.aspnetzero.com/Documents/Getting-Started-Angular

## Production Build

### Optimized Production Configuration

This project includes optimized production build configurations for best performance:

- **Build Optimization**: Enabled AOT compilation, tree-shaking, and minification
- **Bundle Analysis**: Use `npm run build:analyze` to analyze bundle sizes
- **Compression**: Gzip compression enabled in Docker deployment
- **Service Worker**: PWA support with optimized caching strategies
- **Strict TypeScript**: Enhanced type checking for better code quality
- **Performance Budgets**: Set to 2MB initial bundle, 3MB max

### Build Commands

```bash
# Production build
npm run build:prod

# Analyze bundle sizes
npm run build:analyze

# Build with size optimization check
npm run build:optimize
```

### Docker Deployment

```bash
# Build Docker image
docker build -t angular-app .

# Run container
docker run -p 80:80 angular-app
```

### Performance Optimizations

- Critical CSS inlining
- Font optimization
- Asset caching with service worker
- API response caching
- Gzip compression
- Lazy loading support
