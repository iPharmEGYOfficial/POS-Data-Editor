# POS Data Editor v1.0 Stable

**Owner:** Haitham Osama Abdelghaffar | iPharmEGY  
**Product:** POS Data Editor  
**Platform:** Windows Desktop (.NET 8 / WinForms)  

## Purpose
POS Data Editor is a Windows desktop admin tool for controlled POS invoice item updates. It supports selecting an invoice, selecting an item from that invoice only, previewing changes, then updating item price and quantity safely.

## Core features
- Direct SQL Server connection.
- Invoice synchronization.
- Invoice-item-only selection.
- Price update.
- Quantity update.
- Preview before execution.
- Confirmation and execution log.
- Arabic RTL user interface.
- Branded with Haitham Osama Abdelghaffar | iPharmEGY.

## Important note
This is a Windows application, not an Android application. It should be distributed as a Windows installer/portable release, not Google Play.

## Recommended public download location
Use **GitHub Releases** for a clean public download page, release notes, version history, and downloadable ZIP/EXE assets.

## Build command
Run on Windows with .NET 8 SDK installed:

```powershell
.\publish-windows.ps1
```

The output will be created under:

```text
release\POS_Data_Editor_v1.0.0_win-x64
release\POS_Data_Editor_v1.0.0_win-x64.zip
```
