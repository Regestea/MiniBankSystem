declare module "*.module.css" {
  const classes: Record<string, string>;
  export default classes;
}

// Plain (global) stylesheets imported for side effects, e.g. theme.css.
// TypeScript 7 reports TS2882 for undeclared side-effect imports, so every
// CSS pattern the app touches needs an ambient declaration.
declare module "*.css";
