# Population Exposure To Ultrafine Particles: Size-Resolved And Real-Time Models

This package is a cleaned work-required package for Zhu, Marshall, and Levinson (2016), *Population Exposure to Ultrafine Particles: Size-Resolved and Real-Time Models*, Transportation Research Part D, DOI: 10.1016/j.trd.2016.09.010.

## Current Status

This is not ready for a public upload as a complete reproduction package. The paper used size-resolved ultrafine-particle concentration models, MnDOT detector traffic conditions, and individual GPS travel trajectories. The trajectory-level material requires privacy/restricted-review handling, and the named model-input CSVs were not found in the local search.

## What Is Included

- paper/Emissions-Published.pdf: final paper reference copy.
- code/ufp-emission-model-csharp/: source-only Visual Studio 2008 C# UFP emission model found locally. Build outputs and binaries are excluded.
- metadata/SOURCE_FILE_DECISIONS.csv: inclusion/exclusion decisions for reviewed source locations.
- metadata/PACKAGE_FILE_MANIFEST.csv: retained package file manifest.

## Missing Or Restricted Inputs

The C# Program.cs references old F: drive input paths for LoopStationIDPairs.csv, ModelParameters.csv, WeatherCondition.csv, Station_DetID_List.csv, and MnDOT detector volume/speed files. Targeted searches found PDFs/figures and drafts but did not find those named CSV inputs. The GPS travel trajectories are not staged and should not be public without an approved de-identification/restricted-annex decision.

## Nonarchival Files Excluded

Letters, drafts, generated build artifacts, binaries, and reviewer/admin files are excluded. The staged code is source-only.

<!-- package-hardening-status:start -->
## Package Hardening Status

Generated: 2026-05-21 20:19:00 AEST

- Pipeline: `READY-TO-UPLOAD/PUBLIC`
- Sidecars added/updated: `PACKAGE_STATUS.md`, `PACKAGE_MANIFEST.csv`, `LICENSE_STATUS.md`.
- Paper reference copies are for local audit convenience and are not public-upload assets without rights review.
- Final GitHub upload should use the manifest include statuses and the license-status note.
<!-- package-hardening-status:end -->
