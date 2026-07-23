# Changelog

All notable changes to WiseAuth are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Initial pre-release of WiseAuth: an ASP.NET Core authorization library that uses
  power-of-two bit-flag enums and bitwise claim checking to enforce per-endpoint permissions.
- `[EndpointId<T>(value)]` attribute for attribute-routed `[ApiController]` actions, as an
  alternative to the minimal-API `.EndpointId<T>()` extension method.

### Changed

- `WiseAuthorizationHandler<T>` now reads its requirement from the authorization context's
  pending requirements instead of `HttpContext.GetEndpoint()` metadata, so it works uniformly
  for minimal API endpoints and `[ApiController]` actions.
