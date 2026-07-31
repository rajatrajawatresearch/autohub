import type { NextConfig } from 'next';

const config: NextConfig = {
  reactStrictMode: true,

  // The design system and form renderer ship as source, not as a build artefact. Transpiling them
  // here keeps one build step for the whole monorepo and keeps HMR working across package edges.
  transpilePackages: ['@autohub/ui', '@autohub/api', '@autohub/forms'],

  typedRoutes: true,

  images: {
    // Vehicle photography is the heaviest thing on every page. AVIF first, then WebP.
    formats: ['image/avif', 'image/webp'],
    remotePatterns: [
      { protocol: 'https', hostname: '**.autohub.in' },
      { protocol: 'http', hostname: 'localhost' },
    ],
  },

  experimental: {
    optimizePackageImports: ['lucide-react'],
  },
};

export default config;
