# SaverSearch - Project Status Review

**Date:** 7 July 2026

## Purpose

This document records the current state of the project, what has been completed, what remains outstanding, and the blockers discovered during research.

The intention is to provide a clean restart point in the future.

---

# Original Product Vision

A completely free Windows desktop application that helps users discover the best way to save money when shopping.

Constraints established at the beginning of the project:

* Free for end users.
* Built by a solo developer.
* No cloud infrastructure.
* No paid APIs.
* No subscriptions.
* Minimal operational overhead.
* Users install the application and immediately use it.
* No requirement for users to create third-party accounts.

These constraints remain unchanged.

---

# Completed Work

## Core Platform

Completed.

* Solution architecture
* Dependency Injection
* SQLite database
* Domain model
* Repository layer
* Automated testing
* Configuration system

---

## Discovery Engine

Completed.

Includes:

* Retailer Resolution
* Offer Resolution
* Rules Engine
* Savings Calculator
* Offer Normalisation
* Ranking Engine
* Purchase Planning
* Recommendation Engine

---

## Acquisition Framework

Completed.

Includes:

* Connector architecture
* Acquisition pipeline
* Validation
* Normalisation
* Deduplication
* Import jobs
* Upsert engine
* Diagnostics
* Performance testing

The framework itself is reusable regardless of the eventual data source.

---

## Connector Framework

Completed.

Supports provider plug-ins.

New connectors can be added without modifying the framework.

---

## Automated Testing

Extensive automated test suite implemented.

All reported tests passing.

---

# Outstanding Work

## User Interface

Not started.

Current system has no user interface.

This is one of the largest missing pieces.

The intended MVP interface should remain extremely simple.

Example workflow:

Launch application

↓

Synchronise latest offers

↓

Search retailer

↓

Enter purchase amount

↓

Receive recommendation

---

## Packaging

Executable packaging still required.

Goal:

Single Windows executable suitable for beta testers.

---

## End-to-End Validation

Not completed.

No complete user workflow has yet been demonstrated.

---

# Major Research Findings

## Assumption Tested

Original assumption:

"A free automated source of UK shopping offers exists."

Research indicates this assumption is incorrect.

No single comprehensive public source exists.

---

# AWIN

Research identified AWIN as the most technically complete source.

Advantages:

* Structured API
* Cashback information
* Voucher codes
* Merchant data
* Promotion information
* Expiry dates
* Terms

However...

Using AWIN requires participation in the affiliate ecosystem.

Requirements include:

* Publisher account registration.
* Identity verification.
* Approximately £5 publisher verification deposit (refundable under AWIN's current process).
* Generation and management of API credentials.
* Applying to merchant affiliate programmes.
* Ongoing maintenance of affiliate relationships.
* Merchant approvals (some automatic, some manual).

This conflicts with the original project goal of avoiding commercial relationships and operational overhead.

For that reason AWIN is **not considered suitable as the primary MVP data source under the current project constraints.**

The existing AWIN connector should be archived rather than deleted.

---

# Kelkoo Publisher API

Research identified Kelkoo as another structured source.

Advantages:

* Product data.
* Price comparison.
* Publisher APIs.

Requirements:

* Publisher registration.
* API credentials.
* Participation in Kelkoo's publisher ecosystem.

Although technically free to register, it introduces similar operational dependencies to AWIN.

---

# MoneySavingExpert RSS

Public RSS feeds exist.

Advantages:

* Free.
* Publicly accessible.
* Simple integration.

Disadvantages:

* Community discussion rather than structured offer data.
* Limited coverage.
* Not comprehensive enough to power the application alone.

Suitable only as a supplementary source.

---

# Current Blocker

The primary unresolved problem is no longer technical.

It is data acquisition.

The application requires accurate, current shopping data.

Current research indicates there is no source which simultaneously satisfies all of the following:

* Free.
* Comprehensive.
* Structured.
* Automated.
* No registration.
* No commercial relationship.
* No ongoing maintenance.

This is now the project's primary blocker.

---

# Existing Codebase

The following should be retained.

* Discovery Engine
* Acquisition Framework
* Connector Architecture
* SQLite database
* Domain model
* Recommendation Engine
* Ranking Engine
* Purchase Planning
* Retailer Resolution

These components remain valuable regardless of the eventual data source.

---

# Components to Archive

The AWIN connector should be archived rather than deleted.

Reason:

If the project later becomes commercial, affiliate integrations may become appropriate.

The underlying connector architecture remains valid.

---

# Questions To Resolve Before Development Continues

1. Where should the application's data come from?

2. Can the original product vision be achieved under the existing constraints?

3. If not, which constraint is acceptable to change?

Examples:

* Commercial partnerships
* Limited manual curation
* Browser extension integration
* Cloud infrastructure
* Paid APIs
* Different product scope

Until these questions are answered, further implementation risks building around an unsupported assumption.

---

# Recommended Pause Point

Development should pause here.

Do not implement additional connectors.

Do not build further acquisition features.

Do not extend the recommendation engine.

The architecture is sufficiently mature.

The next phase should begin only after a sustainable data acquisition strategy has been identified.

---

# Current Project Status

Core Architecture:
Complete.

Business Logic:
Complete.

Acquisition Framework:
Complete.

User Interface:
Not started.

Executable Packaging:
Not started.

Real Data Source:
Unresolved.

End-to-End Prototype:
Blocked pending data acquisition strategy.
