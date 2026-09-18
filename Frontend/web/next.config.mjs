/** @type {import('next').NextConfig} */
const nextConfig = {
  transpilePackages: ["@minibank/shared"],
  // Required for Aspire AddNextJsApp publish (standalone Dockerfile).
  // `next dev` is unaffected.
  output: "standalone",
};

export default nextConfig;
